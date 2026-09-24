// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Infrastructure.Storage;
using Xunit;

namespace Arca.Infrastructure.Tests.Storage;

public sealed class LocationTests
{
    const string Spec = "arquitectura-base/emmagatzematge-local";

    static PlatformContext Context(PlatformKind platform, string exeFolder, string? xdg = null) => new(
        platform,
        HomeFolder: Path.Combine(Path.GetTempPath(), "home"),
        LocalAppData: Path.Combine(Path.GetTempPath(), "home", "AppData", "Local"),
        XdgDataHome: xdg,
        ExecutableFolder: exeFolder);

    [Fact]
    [Trait("spec", Spec + ": Ubicación de la base de datos configurable (ubicación por defecto)")]
    public void Default_on_windows_is_under_local_app_data()
    {
        using var exe = new TempDirectory();
        var c = Context(PlatformKind.Windows, exe.Path);

        var locations = DataLocations.Resolve(c);

        Assert.False(locations.IsPortable);
        Assert.Equal(Path.Combine(c.LocalAppData!, "ARCA", "arca.db"), locations.DefaultDatabasePath);
        Assert.Equal(Path.Combine(c.LocalAppData!, "ARCA", "settings.json"), locations.SettingsFile);
    }

    [Fact]
    [Trait("spec", Spec + ": Ubicación de la base de datos configurable (ubicación por defecto)")]
    public void Default_on_macos_is_under_application_support()
    {
        using var exe = new TempDirectory();
        var c = Context(PlatformKind.MacOS, exe.Path);

        var locations = DataLocations.Resolve(c);

        Assert.Equal(Path.Combine(c.HomeFolder, "Library", "Application Support", "ARCA", "arca.db"), locations.DefaultDatabasePath);
    }

    [Fact]
    [Trait("spec", Spec + ": Ubicación de la base de datos configurable (ubicación por defecto)")]
    public void Default_on_linux_uses_xdg_data_home_when_set_and_local_share_otherwise()
    {
        using var exe = new TempDirectory();
        var xdg = Path.Combine(Path.GetTempPath(), "xdg");

        var withXdg = DataLocations.Resolve(Context(PlatformKind.Linux, exe.Path, xdg));
        var without = DataLocations.Resolve(Context(PlatformKind.Linux, exe.Path));

        Assert.Equal(Path.Combine(xdg, "arca", "arca.db"), withXdg.DefaultDatabasePath);
        Assert.Equal(
            Path.Combine(Path.GetTempPath(), "home", ".local", "share", "arca", "arca.db"), without.DefaultDatabasePath);
    }

    [Theory]
    [InlineData(PlatformKind.Windows)]
    [InlineData(PlatformKind.MacOS)]
    [InlineData(PlatformKind.Linux)]
    [Trait("spec", Spec + ": Ubicación de la base de datos configurable (ubicación por defecto)")]
    public void Default_never_points_at_system_wide_or_executable_folders(PlatformKind platform)
    {
        using var exe = new TempDirectory();
        var c = Context(platform, exe.Path);

        var path = DataLocations.Resolve(c).DefaultDatabasePath;

        Assert.StartsWith(c.HomeFolder, path, StringComparison.Ordinal);
        Assert.DoesNotContain("Program Files", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("spec", "arquitectura-base/distribucio-multiplataforma: Versión portable (ejecución portable)")]
    public void Marker_next_to_the_executable_keeps_everything_inside_its_folder()
    {
        using var exe = new TempDirectory();
        File.WriteAllText(exe.File(DataLocations.PortableMarker), string.Empty);

        var locations = DataLocations.Resolve(Context(PlatformKind.Windows, exe.Path));

        Assert.True(locations.IsPortable);
        Assert.StartsWith(exe.Path, locations.DataFolder, StringComparison.Ordinal);
        Assert.StartsWith(exe.Path, locations.SettingsFile, StringComparison.Ordinal);
        Assert.StartsWith(exe.Path, locations.DefaultDatabasePath, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", "arquitectura-base/distribucio-multiplataforma: Versión portable (sin marcador)")]
    public void Without_the_marker_the_user_folders_are_used()
    {
        using var exe = new TempDirectory();

        var locations = DataLocations.Resolve(Context(PlatformKind.MacOS, exe.Path));

        Assert.False(locations.IsPortable);
        Assert.DoesNotContain(exe.Path, locations.DataFolder, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolving_locations_creates_nothing_on_disk()
    {
        using var exe = new TempDirectory();
        var locations = DataLocations.Resolve(Context(PlatformKind.MacOS, exe.Path));

        Assert.False(Directory.Exists(locations.DataFolder));
    }

    [Fact]
    public void The_data_folder_is_created_only_when_asked()
    {
        using var exe = new TempDirectory();
        File.WriteAllText(exe.File(DataLocations.PortableMarker), string.Empty);
        var locations = DataLocations.Resolve(Context(PlatformKind.Linux, exe.Path));

        locations.EnsureDataFolder();

        Assert.True(Directory.Exists(locations.DataFolder));
    }

    [Fact]
    [Trait("spec", Spec + ": Ubicación de la base de datos configurable (ruta configurada)")]
    public void A_configured_path_is_used_and_survives_a_restart()
    {
        using var exe = new TempDirectory();
        using var chosen = new TempDirectory();
        var locations = DataLocations.Resolve(Context(PlatformKind.Windows, exe.Path));
        var configured = chosen.File("centre.db");

        new LocalSettingsStore(locations.SettingsFile).Save(new LocalSettings(configured));
        var afterRestart = new LocalSettingsStore(locations.SettingsFile).Load();

        Assert.Equal(configured, locations.DatabasePathFor(afterRestart));
    }

    [Fact]
    public void Without_a_configured_path_the_default_is_used()
    {
        using var exe = new TempDirectory();
        var locations = DataLocations.Resolve(Context(PlatformKind.Windows, exe.Path));

        Assert.Equal(locations.DefaultDatabasePath, locations.DatabasePathFor(new LocalSettings()));
        Assert.Equal(locations.DefaultDatabasePath, locations.DatabasePathFor(new LocalSettings("  ")));
    }

    [Fact]
    public void Missing_settings_file_gives_defaults()
    {
        using var dir = new TempDirectory();

        Assert.Null(new LocalSettingsStore(dir.File("settings.json")).Load().DatabasePath);
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[1,2,3]")]
    public void Damaged_settings_file_gives_defaults_instead_of_failing(string content)
    {
        using var dir = new TempDirectory();
        File.WriteAllText(dir.File("settings.json"), content);

        Assert.Null(new LocalSettingsStore(dir.File("settings.json")).Load().DatabasePath);
    }

    [Fact]
    public void Unknown_settings_are_ignored()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(dir.File("settings.json"), "{ \"DatabasePath\": \"/x/y.db\", \"FutureSetting\": 42 }");

        Assert.Equal("/x/y.db", new LocalSettingsStore(dir.File("settings.json")).Load().DatabasePath);
    }

    [Fact]
    public void Saving_leaves_no_temporary_file_and_creates_the_folder()
    {
        using var dir = new TempDirectory();
        var file = Path.Combine(dir.Path, "sub", "settings.json");

        new LocalSettingsStore(file).Save(new LocalSettings("/a.db"));

        Assert.Equal(["settings.json"], Directory.GetFiles(Path.GetDirectoryName(file)!).Select(Path.GetFileName));
    }
}
