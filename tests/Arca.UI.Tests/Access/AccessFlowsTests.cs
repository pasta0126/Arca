// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Security;
using Arca.Domain.Common;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Arca.UI.Access;
using Xunit;

namespace Arca.UI.Tests.Access;

public sealed class AccessFlowsTests : IDisposable
{
    const string UnlockSpec = "acces-i-xifrat/contrasenya-del-centre";
    const string KeySpec = "acces-i-xifrat/clau-de-recuperacio";
    const string Password = "riu cadira blau gos";
    const string Newer = "gat ratllat sota pluja";

    static readonly Argon2Parameters _cost = new(1024, 1, 1);
    static readonly ResxLocalizer _localizer = new();

    readonly string _folder = Path.Combine(Path.GetTempPath(), "arca-ui-" + Guid.NewGuid().ToString("N"));

    public AccessFlowsTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    string Database => Path.Combine(_folder, "arca.db");

    /// <summary>The store counts how often the key file is read, which is once per attempt to unlock.</summary>
    sealed class CountingStore : IKeyFileStore
    {
        readonly FileKeyFileStore _inner = new();

        public int Reads { get; private set; }

        public Result<KeyFile> Read(string databasePath)
        {
            Reads++;
            return _inner.Read(databasePath);
        }

        public void Write(string databasePath, KeyFile file) => _inner.Write(databasePath, file);

        public void DiscardPrevious(string databasePath) => _inner.DiscardPrevious(databasePath);
    }

    /// <summary>Plays each form with a script of actions, as a person at the screen would, and records what they saw.</summary>
    sealed class ScriptedPresenter : IFormPresenter
    {
        readonly Queue<Func<AccessFormViewModel, Task>[]> _scripts = new();

        public List<AccessFormViewModel> Shown { get; } = [];

        public void Form(params Func<AccessFormViewModel, Task>[] actions) => _scripts.Enqueue(actions);

        public async Task<FormOutcome> ShowAsync(AccessFormViewModel form, CancellationToken ct)
        {
            Shown.Add(form);
            form.Sink = new FakeSink();
            foreach (var action in _scripts.Dequeue())
            {
                await action(form);
                if (form.Completion.IsCompleted)
                {
                    break;
                }
            }

            Assert.True(form.Completion.IsCompleted, "the script ended with the form still open");
            return await form.Completion;
        }
    }

    sealed class FakeSink : IKeySink
    {
        public string? Copied { get; private set; }

        public string? Printed { get; private set; }

        public Task CopyAsync(string text)
        {
            Copied = text;
            return Task.CompletedTask;
        }

        public Task PrintAsync(string title, IReadOnlyList<string> lines)
        {
            Printed = title + "|" + string.Join("|", lines);
            return Task.CompletedTask;
        }

        public void Cleanup()
        {
        }
    }

    static Func<AccessFormViewModel, Task> Type(params string[] texts) => f =>
    {
        for (var i = 0; i < texts.Length; i++)
        {
            f.Fields[i].Text = texts[i];
        }

        return Task.CompletedTask;
    };

    static Func<AccessFormViewModel, Task> Submit() => f => f.SubmitAsync();

    static Func<AccessFormViewModel, Task> Cancel() => f =>
    {
        f.Cancel();
        return Task.CompletedTask;
    };

    static Func<AccessFormViewModel, Task> Secondary() => f =>
    {
        f.ChooseSecondary();
        return Task.CompletedTask;
    };

    /// <summary>Types the groups asked by the form, read from the key it shows.</summary>
    static Func<AccessFormViewModel, Task> TypeAskedGroups() => f =>
    {
        var groups = f.Secret!.Formatted.Split('-');
        for (var i = 0; i < f.Fields.Count; i++)
        {
            var number = int.Parse(new string(f.Fields[i].Label.Where(char.IsDigit).ToArray()), System.Globalization.CultureInfo.InvariantCulture);
            f.Fields[i].Text = groups[number - 1];
        }

        return Task.CompletedTask;
    };

    (AccessFlows Flows, ScriptedPresenter Presenter, CountingStore Store, AccessService Access) Build()
    {
        var store = new CountingStore();
        var access = new AccessService(new NSecKeyCrypto(), store, _cost);
        var presenter = new ScriptedPresenter();
        var flows = new AccessFlows(access, presenter, _localizer, (path, created, groups, ct) => DatabaseCreator.CreateAsync(path, created, groups, ct));
        return (flows, presenter, store, access);
    }

    /// <summary>Runs the first run through the flows and returns the recovery key that the screen showed.</summary>
    async Task<string> FirstRunAsync(string password)
    {
        var (flows, presenter, _, _) = Build();
        presenter.Form(Type(password, password), Submit());
        presenter.Form(TypeAskedGroups(), Submit());

        var result = await flows.CreateAsync(Database, CancellationToken.None);

        Assert.True(result.IsSuccess);
        return presenter.Shown[1].Secret!.Formatted.Replace("-", string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Se establece al empezar)")]
    public async Task First_run_asks_for_the_password_then_the_key_and_creates_the_data()
    {
        var (flows, presenter, _, access) = Build();
        presenter.Form(Type(Password, Password), Submit());
        presenter.Form(TypeAskedGroups(), Submit());

        var result = await flows.CreateAsync(Database, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Database));
        Assert.True(File.Exists(KeyFileStore.PathFor(Database)));
        Assert.Equal(result.Value!.ToArray(), access.Unlock(Database, Password).Value!.ToArray());
        Assert.Equal(["Crea la contrasenya del centre", "Desa la clau de recuperació"], presenter.Shown.Select(f => f.Title));
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Contador de longitud)")]
    public async Task The_password_form_counts_the_characters_against_the_minimum_as_they_are_typed()
    {
        var (flows, presenter, _, _) = Build();
        IReadOnlyList<string> empty = [];
        IReadOnlyList<string> nine = [];
        presenter.Form(
            f =>
            {
                empty = f.Hints;
                f.Fields[0].Text = "riu gos ab";
                nine = f.Hints;
                return Task.CompletedTask;
            },
            Cancel());

        await flows.CreateAsync(Database, CancellationToken.None);

        Assert.Contains("0 de 12 caràcters", empty);
        Assert.Contains("10 de 12 caràcters", nine);
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Indicador de fortaleza y aviso de pérdida (Aviso visible)")]
    public async Task The_password_form_shows_the_loss_warning_and_the_key_form_too()
    {
        var (flows, presenter, _, _) = Build();
        presenter.Form(Type(Password, Password), Submit());
        presenter.Form(Cancel());

        await flows.CreateAsync(Database, CancellationToken.None);

        Assert.All(presenter.Shown, f => Assert.Contains("no es poden recuperar", f.Warning, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Indicador de fortaleza y aviso de pérdida (Contraseña débil)")]
    public async Task A_single_word_is_accepted_with_a_weak_indicator_and_the_advice()
    {
        var (flows, presenter, _, _) = Build();
        IReadOnlyList<string> hints = [];
        presenter.Form(
            f =>
            {
                f.Fields[0].Text = "insmonturiol";
                f.Fields[1].Text = "insmonturiol";
                hints = f.Hints;
                return f.SubmitAsync();
            });
        presenter.Form(Cancel());

        await flows.CreateAsync(Database, CancellationToken.None);

        Assert.Contains("Fortalesa: fluixa", hints);
        Assert.Contains(hints, h => h.Contains("diverses paraules", StringComparison.Ordinal));
        Assert.Equal(2, presenter.Shown.Count); // it went on to the key
    }

    [Theory]
    [Trait("spec", UnlockSpec + ": Contraseña obligatoria de al menos 12 caracteres en la primera ejecución (Demasiado corta)")]
    [InlineData("xyzzy", "xyzzy", "mínim 12 caràcters")]
    [InlineData("contrasenya1234", "contrasenya1234", "massa habitual")]
    [InlineData("riu cadira blau gos", "una altra cosa", "no coincideixen")]
    [InlineData("", "", "Cal escriure una contrasenya")]
    public async Task A_password_that_does_not_meet_the_rules_shows_why_and_creates_nothing(string password, string again, string expected)
    {
        var (flows, presenter, _, _) = Build();
        string error = string.Empty;
        presenter.Form(
            Type(password, again),
            async f =>
            {
                await f.SubmitAsync();
                error = f.Error;
            },
            Cancel());

        var result = await flows.CreateAsync(Database, CancellationToken.None);

        Assert.Contains(expected, error, StringComparison.Ordinal);
        Assert.DoesNotContain(password.Length > 0 ? password : "\0", error, StringComparison.Ordinal);
        Assert.Equal(KeyErrors.UnlockCancelled.Code, result.Error!.Code);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_folder));
    }

    [Fact]
    [Trait("spec", KeySpec + ": Mostrarla una sola vez y confirmar que se ha guardado (Confirmación incorrecta)")]
    public async Task Wrong_groups_do_not_create_the_data_and_the_key_stays_on_screen()
    {
        var (flows, presenter, _, _) = Build();
        string error = string.Empty;
        string keyBefore = string.Empty;
        string keyAfter = string.Empty;
        presenter.Form(Type(Password, Password), Submit());
        presenter.Form(
            async f =>
            {
                keyBefore = f.Secret!.Formatted;
                f.Fields[0].Text = "XXXXX";
                f.Fields[1].Text = "YYYYY";
                await f.SubmitAsync();
                error = f.Error;
                keyAfter = f.Secret.Formatted;
            },
            TypeAskedGroups(),
            Submit());

        var result = await flows.CreateAsync(Database, CancellationToken.None);

        Assert.Contains("no coincideixen", error, StringComparison.Ordinal);
        Assert.Equal(keyBefore, keyAfter);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    [Trait("spec", KeySpec + ": Mostrarla una sola vez y confirmar que se ha guardado (Sin confirmar)")]
    public async Task Continuing_without_typing_the_groups_is_refused_and_explained()
    {
        var (flows, presenter, _, _) = Build();
        string error = string.Empty;
        presenter.Form(Type(Password, Password), Submit());
        presenter.Form(
            async f =>
            {
                await f.SubmitAsync();
                error = f.Error;
            },
            Cancel());

        var result = await flows.CreateAsync(Database, CancellationToken.None);

        Assert.Contains("has de confirmar", error, StringComparison.Ordinal);
        Assert.False(File.Exists(Database));
        Assert.Equal(KeyErrors.UnlockCancelled.Code, result.Error!.Code);
    }

    [Fact]
    [Trait("spec", KeySpec + ": Mostrarla una sola vez y confirmar que se ha guardado (Mostrar la clave)")]
    public async Task The_key_is_shown_in_groups_with_copy_and_print()
    {
        var (flows, presenter, _, _) = Build();
        FakeSink? sink = null;
        string status = string.Empty;
        presenter.Form(Type(Password, Password), Submit());
        presenter.Form(
            async f =>
            {
                sink = (FakeSink)f.Sink!;
                await f.CopyAsync();
                await f.PrintAsync();
                status = f.Status;
            },
            TypeAskedGroups(),
            Submit());

        await flows.CreateAsync(Database, CancellationToken.None);

        var shown = presenter.Shown[1].Secret!.Formatted;
        Assert.Matches("^[0-9A-Z]{5}(-[0-9A-Z]{5}){3}-[0-9A-Z]{6}$", shown);
        Assert.Equal(shown, sink!.Copied);
        Assert.Contains(shown, sink.Printed, StringComparison.Ordinal);
        Assert.Contains("fora de l'ordinador", presenter.Shown[1].Intro, StringComparison.Ordinal);
        Assert.Contains("imprimeixis", status, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Contraseña al abrir la aplicación (Cancelar)")]
    public async Task Giving_up_the_first_run_creates_nothing_and_closes_the_application()
    {
        var (flows, presenter, _, _) = Build();
        presenter.Form(Cancel());

        var result = await flows.CreateAsync(Database, CancellationToken.None);

        Assert.Equal(KeyErrors.UnlockCancelled.Code, result.Error!.Code);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_folder));
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Contraseña al abrir la aplicación (Contraseña incorrecta)")]
    public async Task A_wrong_password_says_so_stays_open_and_the_right_one_opens()
    {
        await FirstRunAsync(Password);
        var (flows, presenter, _, _) = Build();
        string error = string.Empty;
        string typedAfter = "x";
        presenter.Form(
            Type("una altra contrasenya"),
            async f =>
            {
                await f.SubmitAsync();
                error = f.Error;
                typedAfter = f.Fields[0].Text;
            },
            Type(Password),
            Submit());

        var result = await flows.UnlockAsync(Database, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("La contrasenya o la clau no són correctes. Torna-ho a provar.", error);
        Assert.DoesNotContain("una altra", error, StringComparison.Ordinal);
        Assert.Equal(string.Empty, typedAfter);
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Feedback y guía (Doble Intro)")]
    public async Task Submitting_twice_at_once_tries_the_password_a_single_time()
    {
        await FirstRunAsync(Password);
        var (flows, presenter, store, _) = Build();
        presenter.Form(Type(Password), f => Task.WhenAll(f.SubmitAsync(), f.SubmitAsync()));

        var result = await flows.UnlockAsync(Database, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, store.Reads);
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Contraseña al abrir la aplicación (Cancelar)")]
    public async Task Cancelling_the_password_closes_the_application_without_opening_the_data()
    {
        await FirstRunAsync(Password);
        var (flows, presenter, store, _) = Build();
        presenter.Form(Cancel());

        var result = await flows.UnlockAsync(Database, CancellationToken.None);

        Assert.Equal(KeyErrors.UnlockCancelled.Code, result.Error!.Code);
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Contraseña al abrir la aplicación (Ofrecer la recuperación)")]
    public async Task The_unlock_form_offers_the_recovery_key()
    {
        await FirstRunAsync(Password);
        var (flows, presenter, _, _) = Build();
        presenter.Form(Secondary());
        presenter.Form(Cancel()); // recovery form: going back
        presenter.Form(Cancel()); // unlock form again: give up

        await flows.UnlockAsync(Database, CancellationToken.None);

        Assert.Equal("He oblidat la contrasenya", presenter.Shown[0].SecondaryLabel);
        Assert.Equal("Entra amb la clau de recuperació", presenter.Shown[1].Title);
        Assert.Equal(presenter.Shown[0].Title, presenter.Shown[2].Title);
    }

    [Fact]
    [Trait("spec", KeySpec + ": Entrar y restablecer con la clave de recuperación (Nueva clave tras el restablecimiento)")]
    public async Task The_recovery_key_opens_then_asks_for_a_new_password_and_a_new_key()
    {
        var recovery = await FirstRunAsync(Password);
        var (flows, presenter, _, access) = Build();
        var original = access.Unlock(Database, Password).Value!.ToArray();
        presenter.Form(Secondary());
        presenter.Form(Type(recovery.ToLowerInvariant()), Submit()); // tolerant: lower case
        presenter.Form(Type(Newer, Newer), Submit());
        presenter.Form(TypeAskedGroups(), Submit());

        var result = await flows.UnlockAsync(Database, CancellationToken.None);

        Assert.Equal(original, result.Value!.ToArray());
        Assert.Equal(
            ["Contrasenya del centre", "Entra amb la clau de recuperació", "Tria una contrasenya nova", "Desa la clau de recuperació"],
            presenter.Shown.Select(f => f.Title));
        Assert.Equal(original, access.Unlock(Database, Newer).Value!.ToArray());
        Assert.Equal("Keys.WrongCredentials", access.Unlock(Database, Password).Error!.Code);
        var newKey = presenter.Shown[3].Secret!.Formatted.Replace("-", string.Empty, StringComparison.Ordinal);
        Assert.NotEqual(recovery, newKey);
        Assert.True(access.CheckRecoveryKey(Database, newKey).IsSuccess);
        Assert.False(access.CheckRecoveryKey(Database, recovery).IsSuccess);
    }

    [Fact]
    [Trait("spec", KeySpec + ": Entrar y restablecer con la clave de recuperación (Clave incorrecta)")]
    public async Task A_wrong_recovery_key_is_explained_without_revealing_anything_and_can_be_retyped()
    {
        var recovery = await FirstRunAsync(Password);
        var (flows, presenter, _, _) = Build();
        var errors = new List<string>();
        presenter.Form(Secondary());
        presenter.Form(
            Type("no és una clau"),
            async f =>
            {
                await f.SubmitAsync();
                errors.Add(f.Error);
            },
            Type(RecoveryKey.Generate()),
            async f =>
            {
                await f.SubmitAsync();
                errors.Add(f.Error);
            },
            Type(recovery),
            Submit());
        presenter.Form(Cancel()); // new password: back out
        presenter.Form(Cancel()); // unlock again: give up

        await flows.UnlockAsync(Database, CancellationToken.None);

        Assert.Contains("no és vàlida", errors[0], StringComparison.Ordinal);
        Assert.Equal("La contrasenya o la clau no són correctes. Torna-ho a provar.", errors[1]);
    }

    [Fact]
    [Trait("spec", KeySpec + ": Entrar y restablecer con la clave de recuperación (Nueva clave tras el restablecimiento)")]
    public async Task Backing_out_of_a_recovery_before_confirming_the_new_key_changes_nothing()
    {
        var recovery = await FirstRunAsync(Password);
        var (flows, presenter, _, access) = Build();
        var before = File.ReadAllBytes(KeyFileStore.PathFor(Database));
        presenter.Form(Secondary());
        presenter.Form(Type(recovery), Submit());
        presenter.Form(Type(Newer, Newer), Submit());
        presenter.Form(Cancel()); // the new key is never confirmed
        presenter.Form(Cancel()); // unlock again: give up

        await flows.UnlockAsync(Database, CancellationToken.None);

        Assert.Equal(before, File.ReadAllBytes(KeyFileStore.PathFor(Database)));
        Assert.True(access.Unlock(Database, Password).IsSuccess);
        Assert.True(access.CheckRecoveryKey(Database, recovery).IsSuccess);
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Contraseña al abrir la aplicación (Contraseña correcta)")]
    public async Task A_damaged_key_file_found_while_unlocking_ends_the_form_with_its_own_error()
    {
        await FirstRunAsync(Password);
        var (flows, presenter, _, _) = Build();
        File.WriteAllText(KeyFileStore.PathFor(Database), "{ not json");
        presenter.Form(Type(Password), Submit());

        var result = await flows.UnlockAsync(Database, CancellationToken.None);

        Assert.Equal("Keys.FileDamaged", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", UnlockSpec + ": Cambiar la contraseña (Cambio correcto)")]
    public async Task Changing_the_password_from_settings_asks_for_the_current_and_the_new_one()
    {
        await FirstRunAsync(Password);
        var (flows, presenter, _, access) = Build();
        var key = access.Unlock(Database, Password).Value!.ToArray();
        IReadOnlyList<string> hints = [];
        presenter.Form(
            f =>
            {
                f.Fields[0].Text = Password;
                f.Fields[1].Text = "insmonturiol";
                hints = f.Hints;
                f.Fields[1].Text = Newer;
                f.Fields[2].Text = Newer;
                return Task.CompletedTask;
            },
            Submit());

        var result = await flows.ChangePasswordAsync(Database, CancellationToken.None);

        Assert.True(result!.IsSuccess);
        Assert.Contains(result.Notices, n => n.Code == "Keys.BackupsKeepOldPassword");
        Assert.Contains("Fortalesa: fluixa", hints);
        Assert.Equal(key, access.Unlock(Database, Newer).Value!.ToArray());
        Assert.Equal("Keys.WrongCredentials", access.Unlock(Database, Password).Error!.Code);
    }

    [Theory]
    [Trait("spec", UnlockSpec + ": Cambiar la contraseña (Contraseña actual incorrecta)")]
    [InlineData("una altra contrasenya", "gat ratllat sota pluja", "no són correctes")]
    [InlineData("riu cadira blau gos", "xyzzy", "mínim 12 caràcters")]
    public async Task A_change_that_is_refused_says_why_and_leaves_the_password_as_it_was(string current, string next, string expected)
    {
        await FirstRunAsync(Password);
        var (flows, presenter, _, access) = Build();
        var before = File.ReadAllBytes(KeyFileStore.PathFor(Database));
        string error = string.Empty;
        presenter.Form(
            Type(current, next, next),
            async f =>
            {
                await f.SubmitAsync();
                error = f.Error;
            },
            Cancel());

        var result = await flows.ChangePasswordAsync(Database, CancellationToken.None);

        Assert.Null(result);
        Assert.Contains(expected, error, StringComparison.Ordinal);
        Assert.Equal(before, File.ReadAllBytes(KeyFileStore.PathFor(Database)));
        Assert.True(access.Unlock(Database, Password).IsSuccess);
    }

    [Fact]
    [Trait("spec", KeySpec + ": Regenerar la clave (Regenerar)")]
    public async Task Regenerating_asks_for_the_password_then_shows_and_confirms_a_new_key()
    {
        var recovery = await FirstRunAsync(Password);
        var (flows, presenter, _, access) = Build();
        presenter.Form(Type(Password), Submit());
        presenter.Form(TypeAskedGroups(), Submit());

        var result = await flows.RegenerateKeyAsync(Database, CancellationToken.None);

        Assert.True(result!.IsSuccess);
        var newKey = presenter.Shown[1].Secret!.Formatted.Replace("-", string.Empty, StringComparison.Ordinal);
        Assert.NotEqual(recovery, newKey);
        Assert.True(access.CheckRecoveryKey(Database, newKey).IsSuccess);
        Assert.False(access.CheckRecoveryKey(Database, recovery).IsSuccess);
        Assert.Contains("no es podrà tornar a veure", presenter.Shown[1].Intro, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", KeySpec + ": Regenerar la clave (Cancelar antes de confirmar)")]
    public async Task Cancelling_a_regeneration_keeps_the_previous_key_valid()
    {
        var recovery = await FirstRunAsync(Password);
        var (flows, presenter, _, access) = Build();
        var before = File.ReadAllBytes(KeyFileStore.PathFor(Database));
        presenter.Form(Type(Password), Submit());
        presenter.Form(Cancel());

        var result = await flows.RegenerateKeyAsync(Database, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(before, File.ReadAllBytes(KeyFileStore.PathFor(Database)));
        Assert.True(access.CheckRecoveryKey(Database, recovery).IsSuccess);
    }

    [Fact]
    [Trait("spec", KeySpec + ": Regenerar la clave (Regenerar)")]
    public async Task Regenerating_with_a_wrong_password_is_refused_before_showing_any_key()
    {
        await FirstRunAsync(Password);
        var (flows, presenter, _, _) = Build();
        string error = string.Empty;
        presenter.Form(
            Type(Newer),
            async f =>
            {
                await f.SubmitAsync();
                error = f.Error;
            },
            Cancel());

        var result = await flows.RegenerateKeyAsync(Database, CancellationToken.None);

        Assert.Null(result);
        Assert.Contains("no són correctes", error, StringComparison.Ordinal);
        Assert.Single(presenter.Shown);
    }
}
