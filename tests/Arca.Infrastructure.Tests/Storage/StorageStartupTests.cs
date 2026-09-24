// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Startup;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Infrastructure.Storage;
using Arca.Testing;
using Xunit;

namespace Arca.Infrastructure.Tests.Storage;

public sealed class StorageStartupTests
{
    sealed class FixedKey(string seed) : IDatabaseKeyProvider
    {
        public Task<Result<DatabaseKey>> GetKeyAsync(CancellationToken ct = default) =>
            Task.FromResult(Result<DatabaseKey>.Success(TestKeys.FromSeed(seed)));
    }

    sealed class NoKey : IDatabaseKeyProvider
    {
        public Task<Result<DatabaseKey>> GetKeyAsync(CancellationToken ct = default) =>
            Task.FromResult(Result<DatabaseKey>.Failure(StorageErrors.KeyNotAvailable));
    }

    static PlatformContext Machine(TempDirectory home) => new(
        PlatformKind.Linux, home.Path, null, Path.Combine(home.Path, "xdg"), Path.Combine(home.Path, "exe"));

    static StorageStartup Startup(TempDirectory home, IDatabaseKeyProvider keys, bool create = false) =>
        new(Machine(home), keys, create);

    [Fact]
    public async Task First_start_in_development_creates_the_database_in_the_default_folder()
    {
        using var home = new TempDirectory();

        var result = await Startup(home, new FixedKey("a"), create: true).OpenAsync();

        Assert.True(result.IsSuccess);
        await using var session = result.Value!;
        Assert.True(session.WasCreated);
        Assert.Equal(Path.Combine(home.Path, "xdg", "arca", "arca.db"), session.DatabasePath);
        Assert.EndsWith("_InitialCreate", session.SchemaVersion, StringComparison.Ordinal);
        Assert.True(File.Exists(session.DatabasePath));
    }

    [Fact]
    public async Task A_later_start_opens_the_same_database_and_reports_its_schema()
    {
        using var home = new TempDirectory();
        await (await Startup(home, new FixedKey("b"), create: true).OpenAsync()).Value!.DisposeAsync();

        var result = await Startup(home, new FixedKey("b")).OpenAsync();

        Assert.True(result.IsSuccess);
        await using var session = result.Value!;
        Assert.False(session.WasCreated);
        Assert.EndsWith("_InitialCreate", session.SchemaVersion, StringComparison.Ordinal);
        Assert.Equal("1.2.3", session.Info("1.2.3").ApplicationVersion);
    }

    [Fact]
    [Trait("spec", "arquitectura-base/emmagatzematge-local: Base de datos local en un único fichero (primer arranque sin datos)")]
    public async Task Without_a_database_nothing_is_created_and_the_missing_file_is_reported()
    {
        using var home = new TempDirectory();

        var result = await Startup(home, new FixedKey("c")).OpenAsync();

        Assert.Equal("Storage.FileNotFound", result.Error!.Code);
        Assert.False(File.Exists(Path.Combine(home.Path, "xdg", "arca", "arca.db")));
    }

    [Fact]
    public async Task Without_a_key_startup_fails_and_frees_the_lock()
    {
        using var home = new TempDirectory();

        var first = await Startup(home, new NoKey(), create: true).OpenAsync();
        var second = await Startup(home, new FixedKey("d"), create: true).OpenAsync();

        Assert.Equal("Storage.KeyNotAvailable", first.Error!.Code);
        Assert.True(second.IsSuccess); // the failed start did not keep the instance lock
        await second.Value!.DisposeAsync();
    }

    [Fact]
    [Trait("spec", "arquitectura-base/emmagatzematge-local: Instancia única sobre una base de datos")]
    public async Task A_second_start_on_the_same_data_is_refused_until_the_first_ends()
    {
        using var home = new TempDirectory();
        var first = (await Startup(home, new FixedKey("e"), create: true).OpenAsync()).Value!;

        var second = await Startup(home, new FixedKey("e")).OpenAsync();
        await first.DisposeAsync();
        var third = await Startup(home, new FixedKey("e")).OpenAsync();

        Assert.Equal("Storage.AlreadyRunning", second.Error!.Code);
        Assert.True(third.IsSuccess);
        await third.Value!.DisposeAsync();
    }

    [Fact]
    [Trait("spec", "arquitectura-base/emmagatzematge-local: Fichero corrupto o clave incompatible")]
    public async Task A_damaged_file_stops_startup_untouched_and_frees_the_lock()
    {
        using var home = new TempDirectory();
        var folder = Path.Combine(home.Path, "xdg", "arca");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "arca.db");
        await File.WriteAllTextAsync(file, "garbage".PadRight(4096, 'x'));
        var before = await File.ReadAllBytesAsync(file);

        var result = await Startup(home, new FixedKey("f")).OpenAsync();
        var again = await Startup(home, new FixedKey("f")).OpenAsync();

        Assert.Equal("Storage.Unreadable", result.Error!.Code);
        Assert.Equal("Storage.Unreadable", again.Error!.Code); // not AlreadyRunning: the lock was released
        Assert.Equal(before, await File.ReadAllBytesAsync(file));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/emmagatzematge-local: Ubicación de la base de datos configurable (ruta configurada)")]
    public async Task A_configured_path_is_used_instead_of_the_default_one()
    {
        using var home = new TempDirectory();
        using var chosen = new TempDirectory();
        var locations = DataLocations.Resolve(Machine(home));
        new LocalSettingsStore(locations.SettingsFile).Save(new LocalSettings(chosen.File("centre.db")));

        var result = await Startup(home, new FixedKey("g"), create: true).OpenAsync();

        Assert.True(result.IsSuccess);
        await using var session = result.Value!;
        Assert.Equal(chosen.File("centre.db"), session.DatabasePath);
        Assert.False(File.Exists(locations.DefaultDatabasePath));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/emmagatzematge-local: Ubicación de la base de datos configurable (ruta no accesible)")]
    public async Task A_configured_path_in_a_missing_folder_is_reported_and_nothing_is_created()
    {
        using var home = new TempDirectory();
        using var chosen = new TempDirectory();
        var locations = DataLocations.Resolve(Machine(home));
        var missing = Path.Combine(chosen.Path, "gone", "centre.db");
        new LocalSettingsStore(locations.SettingsFile).Save(new LocalSettings(missing));

        var result = await Startup(home, new FixedKey("h"), create: true).OpenAsync();

        Assert.Equal("Storage.PathNotAccessible", result.Error!.Code);
        Assert.False(Directory.Exists(Path.Combine(chosen.Path, "gone")));
        Assert.Empty(Directory.GetFileSystemEntries(chosen.Path));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/distribucio-multiplataforma: Versión portable (ejecución portable)")]
    public async Task Portable_startup_writes_only_inside_the_executable_folder()
    {
        using var home = new TempDirectory();
        var exe = Path.Combine(home.Path, "exe");
        Directory.CreateDirectory(exe);
        await File.WriteAllTextAsync(Path.Combine(exe, DataLocations.PortableMarker), string.Empty);

        var result = await Startup(home, new FixedKey("i"), create: true).OpenAsync();

        Assert.True(result.IsSuccess);
        await using var session = result.Value!;
        Assert.StartsWith(exe, session.DatabasePath, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(home.Path, "xdg"))); // nothing outside the folder
    }

    sealed class ThrowingKey : IDatabaseKeyProvider
    {
        public Task<Result<DatabaseKey>> GetKeyAsync(CancellationToken ct = default) =>
            throw new InvalidOperationException("boom");
    }

    [Fact]
    public async Task An_unexpected_failure_still_frees_the_instance_lock()
    {
        using var home = new TempDirectory();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Startup(home, new ThrowingKey(), create: true).OpenAsync());
        var retry = await Startup(home, new FixedKey("retry"), create: true).OpenAsync();

        Assert.True(retry.IsSuccess, retry.Error?.Code); // not AlreadyRunning
        await retry.Value!.DisposeAsync();
    }

    [Fact]
    public async Task A_cancelled_start_frees_the_instance_lock()
    {
        using var home = new TempDirectory();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Startup(home, new FixedKey("c1"), create: true).OpenAsync(ct: cancelled.Token));
        var retry = await Startup(home, new FixedKey("c1"), create: true).OpenAsync();

        Assert.True(retry.IsSuccess, retry.Error?.Code);
        await retry.Value!.DisposeAsync();
    }

    sealed class Collect : IProgress<StartupProgress>
    {
        public List<StartupProgress> Reports { get; } = [];

        public void Report(StartupProgress value) => Reports.Add(value);
    }

    [Fact]
    [Trait("spec", "arquitectura-base/feedback-operacions: Pantalla de arranque con etapas reales (arranque normal)")]
    public async Task Start_reports_the_real_stages_in_order()
    {
        using var home = new TempDirectory();
        var progress = new Collect();

        var result = await Startup(home, new FixedKey("stages"), create: true).OpenAsync(progress);

        Assert.True(result.IsSuccess);
        await result.Value!.DisposeAsync();
        Assert.Equal(
            ["Startup.Stage.Location", "Startup.Stage.Instance", "Startup.Stage.Key", "Startup.Stage.Database"],
            progress.Reports.Select(r => r.TextKey));
        Assert.Equal([1, 2, 3, 4], progress.Reports.Select(r => r.Index));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/feedback-operacions: Pantalla de arranque con etapas reales (arranque con migración)")]
    public async Task A_migrating_start_says_it_is_updating_the_database()
    {
        using var home = new TempDirectory();
        var folder = Path.Combine(home.Path, "xdg", "arca");
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.arcafixture"), Path.Combine(folder, "arca.db"));
        var progress = new Collect();

        var result = await Startup(home, new FixedFixtureKey()).OpenAsync(progress);

        Assert.True(result.IsSuccess, result.Error?.Code);
        await result.Value!.DisposeAsync();
        var keys = progress.Reports.Select(r => r.TextKey).ToList();
        Assert.Equal(["Startup.Stage.BackingUp", "Startup.Stage.Migrating"], keys.Skip(4));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/feedback-operacions: Pantalla de arranque con etapas reales (fallo de arranque)")]
    public async Task A_failing_stage_stops_the_later_ones()
    {
        using var home = new TempDirectory();
        var progress = new Collect();

        var result = await Startup(home, new NoKey(), create: true).OpenAsync(progress);

        Assert.Equal("Storage.KeyNotAvailable", result.Error!.Code);
        Assert.Equal(
            ["Startup.Stage.Location", "Startup.Stage.Instance", "Startup.Stage.Key"], progress.Reports.Select(r => r.TextKey));
    }

    sealed class FixedFixtureKey : IDatabaseKeyProvider
    {
        public Task<Result<DatabaseKey>> GetKeyAsync(CancellationToken ct = default) =>
            Task.FromResult(Result<DatabaseKey>.Success(TestKeys.Fixture()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abcd")]
    [InlineData("zz00000000000000000000000000000000000000000000000000000000000000")]
    public async Task Environment_key_rejects_missing_short_or_non_hex_values(string? value)
    {
        var provider = new EnvironmentKeyProvider(_ => value);

        var result = await provider.GetKeyAsync();

        Assert.Equal("Storage.KeyNotAvailable", result.Error!.Code);
    }

    [Fact]
    public async Task Environment_key_accepts_64_hex_characters()
    {
        var provider = new EnvironmentKeyProvider(name => name == EnvironmentKeyProvider.VariableName ? new string('a', 64) : null);

        var result = await provider.GetKeyAsync();

        Assert.True(result.IsSuccess);
        Assert.All(result.Value!.ToArray(), b => Assert.Equal(0xAA, b));
    }
}
