// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Security;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Arca.Testing;
using Xunit;

namespace Arca.Infrastructure.Tests.Security;

public sealed class AccessServiceTests
{
    const string Spec = "acces-i-xifrat/contrasenya-del-centre";
    const string Password = "riu cadira blau gos";
    const string Other = "gat ratllat sota pluja";

    // Small cost so the tests are quick; the real cost is checked separately.
    static readonly Argon2Parameters _cost = new(1024, 1, 1);

    static AccessService Service(IKeyFileStore? store = null) => new(new NSecKeyCrypto(), store ?? new FileKeyFileStore(), _cost);

    /// <summary>A database path with a key file already written for <see cref="Password"/>; returns the key and the recovery key.</summary>
    static (byte[] Key, string Recovery) Setup(TempDirectory dir, string database, string password = Password)
    {
        using var access = Service().CreateAccess(password, password).Value!;
        KeyFileStore.Write(database, access.KeyFile);
        return (access.DataKey.ToArray(), access.RecoveryKey);
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Contraseña válida)")]
    public void Creating_the_access_returns_the_key_the_recovery_key_and_the_key_file()
    {
        var result = Service().CreateAccess(Password, Password);

        Assert.True(result.IsSuccess);
        using var access = result.Value!;
        Assert.Equal(26, access.RecoveryKey.Length);
        var opened = KeyWrapping.UnwrapWithPassword(new NSecKeyCrypto(), access.KeyFile, PasswordText.ToBytes(Password));
        Assert.Equal(access.DataKey.ToArray(), opened.Value!.ToArray());
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (No coinciden)")]
    public void Different_confirmation_is_refused_and_creates_nothing()
    {
        var result = Service().CreateAccess(Password, Other);

        Assert.Equal("Keys.PasswordMismatch", result.Error!.Code);
        Assert.Null(result.Value);
    }

    [Theory]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Sin contraseña)")]
    [InlineData(null, "Keys.PasswordRequired")]
    [InlineData("", "Keys.PasswordRequired")]
    [InlineData("curta", "Keys.PasswordTooShort")]
    [InlineData("contrasenya1234", "Keys.PasswordTooCommon")]
    public void A_password_that_does_not_meet_the_rules_creates_nothing(string? password, string code)
    {
        Assert.Equal(code, Service().CreateAccess(password, password).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Indicador de fortaleza y aviso de pérdida (Contraseña débil)")]
    public void A_weak_but_long_enough_password_is_accepted_with_the_warning()
    {
        var result = Service().CreateAccess("insmonturiol", "insmonturiol");

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Notices, n => n.Code == "Keys.PasswordWeak");
        result.Value!.Dispose();
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña al abrir la aplicación (Contraseña correcta)")]
    public void Unlocking_with_the_right_password_gives_the_key_and_drops_the_previous_key_file()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, _) = Setup(dir, database);
        KeyFileStore.Write(database, KeyFileStore.Read(database).Value!); // leaves a previous version behind
        Assert.True(File.Exists(KeyFileStore.PreviousPathFor(database)));

        var result = Service().Unlock(database, Password);

        Assert.Equal(key, result.Value!.ToArray());
        Assert.False(File.Exists(KeyFileStore.PreviousPathFor(database)));
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña al abrir la aplicación (Contraseña incorrecta)")]
    public void Unlocking_with_a_wrong_password_says_nothing_more_and_changes_nothing()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(dir, database);
        var before = File.ReadAllBytes(KeyFileStore.PathFor(database));

        var result = Service().Unlock(database, Other);

        Assert.Equal("Keys.WrongCredentials", result.Error!.Code);
        Assert.Empty(result.Error.Args);
        Assert.Equal(before, File.ReadAllBytes(KeyFileStore.PathFor(database)));
    }

    [Theory]
    [Trait("spec", Spec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Caracteres libres)")]
    [InlineData("la porta és tancada")]
    [InlineData("çaragossa i l·lucia")]
    public void Passwords_with_accents_and_special_letters_open_the_same_data(string password)
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, _) = Setup(dir, database, password);

        Assert.Equal(key, Service().Unlock(database, password).Value!.ToArray());
        Assert.Equal(key, Service().Unlock(database, password.Normalize(System.Text.NormalizationForm.FormD)).Value!.ToArray());
    }

    [Fact]
    [Trait("spec", Spec + ": Cambiar la contraseña (Cambio correcto)")]
    public void Changing_the_password_makes_the_new_one_work_and_the_old_one_stop_with_the_same_key()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, _) = Setup(dir, database);

        var result = Service().ChangePassword(database, Password, Other, Other);

        Assert.True(result.IsSuccess);
        Assert.Equal(key, Service().Unlock(database, Other).Value!.ToArray());
        Assert.Equal("Keys.WrongCredentials", Service().Unlock(database, Password).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambiar la contraseña (Aviso sobre las copias)")]
    public void Changing_the_password_reminds_that_earlier_copies_keep_the_old_one()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(dir, database);

        var result = Service().ChangePassword(database, Password, Other, Other);

        Assert.Contains(result.Notices, n => n.Code == "Keys.BackupsKeepOldPassword");
    }

    [Fact]
    [Trait("spec", Spec + ": Cambiar la contraseña (Contraseña actual incorrecta)")]
    public void A_wrong_current_password_refuses_the_change_and_modifies_nothing()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(dir, database);
        var before = File.ReadAllBytes(KeyFileStore.PathFor(database));

        var result = Service().ChangePassword(database, "una altra contrasenya", Other, Other);

        Assert.Equal("Keys.WrongCredentials", result.Error!.Code);
        Assert.Equal(before, File.ReadAllBytes(KeyFileStore.PathFor(database)));
    }

    [Theory]
    [Trait("spec", Spec + ": Cambiar la contraseña (Nueva contraseña que no cumple los requisitos)")]
    [InlineData("curta", "curta", "Keys.PasswordTooShort")]
    [InlineData("contrasenya1234", "contrasenya1234", "Keys.PasswordTooCommon")]
    [InlineData("gat ratllat sota pluja", "una altra cosa ben diferent", "Keys.PasswordMismatch")]
    public void A_new_password_that_does_not_meet_the_rules_changes_nothing(string newPassword, string confirmation, string code)
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(dir, database);
        var before = File.ReadAllBytes(KeyFileStore.PathFor(database));

        var result = Service().ChangePassword(database, Password, newPassword, confirmation);

        Assert.Equal(code, result.Error!.Code);
        Assert.Equal(before, File.ReadAllBytes(KeyFileStore.PathFor(database)));
        Assert.True(Service().Unlock(database, Password).IsSuccess);
    }

    sealed class FailingStore(IKeyFileStore inner) : IKeyFileStore
    {
        public Result<KeyFile> Read(string databasePath) => inner.Read(databasePath);

        public void Write(string databasePath, KeyFile file) => throw new IOException("disk full");

        public void DiscardPrevious(string databasePath) => inner.DiscardPrevious(databasePath);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambiar la contraseña (Fallo a mitad)")]
    public void A_failure_while_writing_leaves_the_old_password_working()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, _) = Setup(dir, database);

        var result = Service(new FailingStore(new FileKeyFileStore())).ChangePassword(database, Password, Other, Other);

        Assert.Equal("Keys.ChangeFailed", result.Error!.Code);
        Assert.Equal(key, Service().Unlock(database, Password).Value!.ToArray());
    }

    [Fact]
    [Trait("spec", Spec + ": Cambiar la contraseña (Fallo a mitad)")]
    public void An_interrupted_write_leaves_no_half_written_key_file()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, _) = Setup(dir, database);
        File.WriteAllText(KeyFileStore.PathFor(database) + ".tmp", "{ half written"); // what a crash would leave

        Assert.Equal(key, Service().Unlock(database, Password).Value!.ToArray());
        Assert.True(Service().ChangePassword(database, Password, Other, Other).IsSuccess);
        Assert.Equal(key, Service().Unlock(database, Other).Value!.ToArray());
    }

    sealed class ScriptedPrompt(params string?[] answers) : IUnlockPrompt
    {
        readonly Queue<string?> _answers = new(answers);

        public List<bool> PreviousFailures { get; } = [];

        public Task<string?> AskPasswordAsync(bool previousFailure, CancellationToken ct)
        {
            PreviousFailures.Add(previousFailure);
            return Task.FromResult(_answers.Dequeue());
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña al abrir la aplicación (Contraseña incorrecta)")]
    public async Task The_unlock_stage_asks_again_after_a_wrong_password_and_then_opens()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var (key, _) = Setup(dir, database);
        var prompt = new ScriptedPrompt(Other, "", Password);

        var result = await new PasswordKeyProvider(Service(), prompt).GetKeyAsync(database);

        Assert.Equal(key, result.Value!.ToArray());
        Assert.Equal([false, true, true], prompt.PreviousFailures);
    }

    [Fact]
    [Trait("spec", Spec + ": Contraseña al abrir la aplicación (Cancelar)")]
    public async Task Cancelling_the_unlock_stops_the_start_without_opening_anything()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(dir, database);

        var result = await new PasswordKeyProvider(Service(), new ScriptedPrompt((string?)null)).GetKeyAsync(database);

        Assert.Equal(KeyErrors.UnlockCancelled.Code, result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Bloqueo sin interrupciones (Arranque)")]
    public async Task The_unlock_stage_runs_inside_the_start_up_sequence_before_the_database_opens()
    {
        using var home = new TempDirectory();
        var platform = new PlatformContext(
            PlatformKind.Linux, home.Path, null, Path.Combine(home.Path, "xdg"), Path.Combine(home.Path, "exe"));
        var database = Path.Combine(home.Path, "xdg", "arca", "arca.db");
        Directory.CreateDirectory(Path.GetDirectoryName(database)!);
        using (var access = Service().CreateAccess(Password, Password).Value!)
        {
            KeyFileStore.Write(database, access.KeyFile);
            var created = await ArcaDatabase.CreateAsync(database, access.DataKey);
            Assert.True(created.IsSuccess);
            await created.Value!.DisposeAsync();
        }

        var progress = new RecordingProgress();
        var prompt = new ScriptedPrompt(Password);

        var result = await new StorageStartup(platform, new PasswordKeyProvider(Service(), prompt)).OpenAsync(progress);

        Assert.True(result.IsSuccess);
        await using var session = result.Value!;
        Assert.Equal(["Startup.Stage.Location", "Startup.Stage.Instance", "Startup.Stage.Key", "Startup.Stage.Database"], progress.Reports.Select(r => r.TextKey));
    }

    [Fact]
    [Trait("spec", Spec + ": La contraseña nunca se guarda ni se registra (Registro técnico)")]
    public async Task A_failed_unlock_leaves_no_trace_of_the_password_in_the_log_errors_or_files()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(dir, database);
        const string Wrong = "contrasenya equivocada única";
        var log = new RecordingErrorLog();

        var direct = Service().Unlock(database, Wrong);
        var staged = await new PasswordKeyProvider(Service(), new ScriptedPrompt(Wrong, null)).GetKeyAsync(database);
        var change = Service().ChangePassword(database, Wrong, Other, Other);

        Assert.Empty(log.Entries);
        foreach (var error in new[] { direct.Error!, staged.Error!, change.Error! })
        {
            Assert.DoesNotContain(Wrong, error.Code, StringComparison.Ordinal);
            Assert.DoesNotContain(error.Args, a => Convert.ToString(a, System.Globalization.CultureInfo.InvariantCulture)!.Contains(Wrong, StringComparison.Ordinal));
        }

        Assert.All(Directory.EnumerateFiles(dir.Path), f =>
            Assert.DoesNotContain(Wrong, System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(f)), StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec + ": La contraseña nunca se guarda ni se registra (Sin persistencia)")]
    public void The_password_is_not_written_to_any_file()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        Setup(dir, database);
        Service().Unlock(database, Password);
        Service().ChangePassword(database, Password, Other, Other);

        foreach (var file in Directory.EnumerateFiles(dir.Path))
        {
            var text = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(file));
            Assert.DoesNotContain(Password, text, StringComparison.Ordinal);
            Assert.DoesNotContain(Other, text, StringComparison.Ordinal);
        }
    }

    sealed class RecordingProgress : IProgress<Arca.Application.Startup.StartupProgress>
    {
        public List<Arca.Application.Startup.StartupProgress> Reports { get; } = [];

        public void Report(Arca.Application.Startup.StartupProgress value) => Reports.Add(value);
    }
}
