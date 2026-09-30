// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Backup;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Application.Security;
using Arca.Domain.Common;
using Arca.Infrastructure.Security;
using Arca.Testing;
using Arca.UI.Access;
using Arca.UI.Backup;
using Arca.UI.Preferences;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Arca.UI.Tests.Backup;

/// <summary>The «Còpia de seguretat» block of Settings (copies-de-seguretat), with the service, the chooser and the forms scripted.</summary>
public sealed class BackupViewModelTests
{
    const string Copy = "copies-de-seguretat/copia-de-seguretat";
    const string Restore = "copies-de-seguretat/restauracio";

    static readonly ResxLocalizer _localizer = new();

    sealed class FakeSession(Result<RestorePreview> preview, Result<bool>? unlock = null) : IRestoreSession
    {
        public bool Disposed { get; private set; }

        public bool Restored { get; private set; }

        public List<string?> Passwords { get; } = [];

        public Result<RestoreOutcome> Outcome { get; set; } = Result<RestoreOutcome>.Success(new RestoreOutcome("previous", false));

        public async Task<Result<bool>> UnlockWithPasswordAsync(string? password, CancellationToken ct = default)
        {
            Passwords.Add(password);
            await Task.Yield();
            return password == "good" ? unlock ?? Result<bool>.Success(true, new Notice("Keys.RestoreAdoptsPassword")) : Result<bool>.Failure(KeyErrors.WrongCredentials);
        }

        public Result<bool> CheckRecoveryKey(string? recoveryKey) => Result<bool>.Failure(KeyErrors.WrongCredentials);

        public Result<PendingKeyChange> BeginRecovery(string? recoveryKey, string? newPassword, string? confirmation) =>
            Result<PendingKeyChange>.Failure(KeyErrors.WrongCredentials);

        public Task<Result<bool>> ConfirmRecoveryAsync(PendingKeyChange pending, IReadOnlyList<string?>? typedGroups, CancellationToken ct = default) =>
            Task.FromResult(Result<bool>.Failure(KeyErrors.WrongCredentials));

        public Task<Result<RestorePreview>> PreviewAsync(CancellationToken ct = default) => Task.FromResult(preview);

        public Task<Result<RestoreOutcome>> RestoreAsync(IProgress<string>? steps = null, CancellationToken ct = default)
        {
            Restored = true;
            return Task.FromResult(Outcome);
        }

        public void Dispose() => Disposed = true;
    }

    sealed class FakeService : IBackupService
    {
        public List<string> Made { get; } = [];

        public bool DestinationExists { get; set; }

        public Result<BackupResult>? Result { get; set; }

        public Func<string, IProgress<string>?, CancellationToken, Task<Result<BackupResult>>>? Work { get; set; }

        public Result<IRestoreSession> Opened { get; set; } = Result<IRestoreSession>.Failure(KeyErrors.BackupDamaged("x"));

        public string SuggestedName(DateTimeOffset moment) => "ARCA-copia-" + moment.ToString("yyyy-MM-dd-HHmm") + ".arcabackup";

        public bool Exists(string destination) => DestinationExists;

        public Task<Result<BackupResult>> BackUpAsync(string destination, IProgress<string>? steps = null, CancellationToken ct = default)
        {
            Made.Add(destination);
            return Work is not null ? Work(destination, steps, ct) : Task.FromResult(Result ?? Result<BackupResult>.Success(new BackupResult(destination, 2_500_000)));
        }

        public Result<IRestoreSession> Open(string file) => Opened;
    }

    sealed class FakePicker : IBackupFilePicker
    {
        public string? Save { get; set; } = "/copies/nova.arcabackup";

        public string? Open { get; set; } = "/copies/vella.arcabackup";

        public List<(string Name, string? Folder)> SaveAsked { get; } = [];

        public List<string?> OpenAsked { get; } = [];

        public Task<string?> PickSaveAsync(string suggestedName, string? folder)
        {
            SaveAsked.Add((suggestedName, folder));
            return Task.FromResult(Save);
        }

        public Task<string?> PickOpenAsync(string? folder)
        {
            OpenAsked.Add(folder);
            return Task.FromResult(Open);
        }
    }

    sealed class FakeRestarter(bool can) : IApplicationRestarter
    {
        public bool Restarted { get; private set; }

        public bool Closed { get; private set; }

        public bool CanRestart => can;

        public void Restart() => Restarted = true;

        public void Close() => Closed = true;
    }

    sealed class ScriptedForms : IFormPresenter
    {
        public Queue<Func<AccessFormViewModel, Task>> Script { get; } = new();

        public List<AccessFormViewModel> Shown { get; } = [];

        public async Task<FormOutcome> ShowAsync(AccessFormViewModel form, CancellationToken ct)
        {
            Shown.Add(form);
            await Script.Dequeue()(form);
            return await form.Completion;
        }
    }

    sealed class InMemoryPreferences : IUiPreferencesStore
    {
        public UiPreferences Saved { get; private set; } = new();

        public UiPreferences Load() => Saved;

        public void Save(UiPreferences preferences) => Saved = preferences;
    }

    sealed class Setup
    {
        public FakeService Service { get; } = new();

        public FakePicker Picker { get; } = new();

        public Arca.Application.Feedback.IConfirmationService Confirmations { get; set; } = new RecordingConfirmations(true);

        public ScriptedForms Forms { get; } = new();

        public RecordingNotifications Notifications { get; } = new();

        public RecordingErrorLog Log { get; } = new();

        public FakeRestarter Restarter { get; set; } = new(true);

        public InMemoryPreferences Store { get; } = new();

        public UiPreferencesSession Preferences { get; }

        public BackupViewModel Model { get; }

        public Setup()
        {
            Preferences = new UiPreferencesSession(Store);
            var access = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore(), new Argon2Parameters(1024, 1, 1));
            var flows = new AccessFlows(access, Forms, _localizer, (_, _, _, _) => Task.FromResult(Result<bool>.Success(true)));
            Model = Build(flows);
        }

        BackupViewModel Build(AccessFlows flows) => new(
            Service, Picker, Confirmations, flows, Notifications, Restarter, Preferences,
            new FakeClock(new DateTimeOffset(2026, 9, 24, 10, 30, 0, TimeSpan.Zero)), _localizer, Log);

    }

    static List<Arca.Application.Feedback.ConfirmationRequest> Asked(Setup setup) => ((RecordingConfirmations)setup.Confirmations).Asked;

    static RestorePreview Preview(SchemaRelation relation = SchemaRelation.Same) =>
        new(new ContentCounts(1, 40, 120, 35), new ContentCounts(2, 55, 120, 50), relation);

    static Func<AccessFormViewModel, Task> TypeAndSubmit(string text) => async f =>
    {
        f.Fields[0].Text = text;
        await f.SubmitAsync();
    };

    // --- Making a backup ---

    [Fact]
    [Trait("spec", Copy + ": Aviso previo; Nombre propuesto; Última carpeta; Copia correcta (Feedback)")]
    public async Task A_backup_warns_about_minors_proposes_the_name_and_the_last_folder_and_tells_where_it_is_and_how_big()
    {
        var setup = new Setup();
        setup.Preferences.SetBackupFolder("/copies");

        await setup.Model.MakeAsync();

        Assert.Equal(_localizer.Get("Backup.Label.MinorsWarning"), Asked(setup)[0].Consequence);
        Assert.Equal(("ARCA-copia-2026-09-24-1030.arcabackup", "/copies"), setup.Picker.SaveAsked.Single());
        Assert.Equal(["/copies/nova.arcabackup"], setup.Service.Made);
        var done = Assert.Single(setup.Notifications.Published);
        Assert.Equal(Arca.Application.Feedback.NotificationKind.Success, done.Kind);
        Assert.Equal(_localizer.Get("Backup.Label.Done", "/copies/nova.arcabackup", _localizer.Get("Backup.Label.SizeMb", "2,5")), done.Text.Replace("2.5", "2,5"));
        Assert.Equal(string.Empty, setup.Model.Status);
        Assert.True(setup.Model.CanAct);
    }

    [Fact]
    [Trait("spec", Copy + ": Última carpeta")]
    public async Task The_folder_of_a_backup_is_remembered_for_the_next_one()
    {
        var setup = new Setup();

        await setup.Model.MakeAsync();
        await setup.Model.MakeAsync();

        Assert.Equal("/copies", setup.Store.Saved.BackupFolder);
        Assert.Equal("/copies", setup.Picker.SaveAsked[1].Folder);
    }

    [Fact]
    [Trait("spec", Copy + ": Aviso previo (Sin confirmar); Cancelación")]
    public async Task Declining_the_warning_or_giving_up_the_chooser_makes_nothing()
    {
        var declined = new Setup { Confirmations = new RecordingConfirmations(false) };
        var model = NewModel(declined);
        await model.MakeAsync();
        Assert.Empty(declined.Service.Made);
        Assert.Empty(declined.Picker.SaveAsked);

        var gaveUp = new Setup();
        gaveUp.Picker.Save = null;
        await gaveUp.Model.MakeAsync();
        Assert.Empty(gaveUp.Service.Made);
        Assert.Empty(gaveUp.Notifications.Published);
    }

    static BackupViewModel NewModel(Setup setup)
    {
        var access = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore(), new Argon2Parameters(1024, 1, 1));
        var flows = new AccessFlows(access, setup.Forms, _localizer, (_, _, _, _) => Task.FromResult(Result<bool>.Success(true)));
        return new BackupViewModel(
            setup.Service, setup.Picker, setup.Confirmations, flows, setup.Notifications, setup.Restarter, setup.Preferences,
            new FakeClock(new DateTimeOffset(2026, 9, 24, 10, 30, 0, TimeSpan.Zero)), _localizer, setup.Log);
    }

    [Fact]
    [Trait("spec", Copy + ": Fichero existente")]
    public async Task An_existing_file_is_only_overwritten_after_confirming_it()
    {
        var setup = new Setup();
        setup.Service.DestinationExists = true;
        await setup.Model.MakeAsync();
        Assert.Equal(2, Asked(setup).Count); // the warning and then the overwrite
        Assert.True(Asked(setup)[1].Destructive);
        Assert.Single(setup.Service.Made);

        var refused = new Setup { Confirmations = new SecondNoConfirmations() };
        refused.Service.DestinationExists = true;
        await NewModel(refused).MakeAsync();
        Assert.Empty(refused.Service.Made);
    }

    sealed class SecondNoConfirmations : Arca.Application.Feedback.IConfirmationService
    {
        int _asked;

        public Task<bool> ConfirmAsync(Arca.Application.Feedback.ConfirmationRequest request, CancellationToken ct = default) => Task.FromResult(++_asked == 1);
    }

    [Fact]
    [Trait("spec", Copy + ": Error comprensible")]
    public async Task A_refusal_is_told_with_its_reason_in_the_language_of_the_person()
    {
        var setup = new Setup();
        setup.Service.Result = Result<BackupResult>.Failure(BackupErrors.DestinationIsDatabase);

        await setup.Model.MakeAsync();

        var shown = Assert.Single(setup.Notifications.Published);
        Assert.Equal(Arca.Application.Feedback.NotificationKind.Error, shown.Kind);
        Assert.Equal(_localizer.Message(BackupErrors.DestinationIsDatabase), shown.Text);
    }

    [Fact]
    [Trait("spec", Copy + ": Doble clic; Progreso y estados")]
    public async Task While_a_backup_runs_a_second_click_does_nothing_and_the_stage_is_shown()
    {
        var setup = new Setup();
        var release = new TaskCompletionSource();
        setup.Service.Work = async (destination, steps, _) =>
        {
            steps!.Report("Backup.Stage.Verifying");
            await release.Task;
            return Result<BackupResult>.Success(new BackupResult(destination, 1000));
        };

        var first = setup.Model.MakeAsync();
        await Task.Delay(100);
        var second = setup.Model.MakeAsync();
        await second;

        Assert.True(setup.Model.IsBusy);
        Assert.False(setup.Model.CanAct);
        Assert.Equal(_localizer.Get("Backup.Stage.Verifying"), setup.Model.Status);
        release.SetResult();
        await first;
        Assert.Single(setup.Service.Made);
        Assert.True(setup.Model.CanAct);
    }

    [Fact]
    [Trait("spec", Copy + ": Cancelación")]
    public async Task Cancelling_a_running_backup_says_so_and_changes_nothing()
    {
        var setup = new Setup();
        setup.Service.Work = async (_, _, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return default!;
        };

        var running = setup.Model.MakeAsync();
        await Task.Delay(100);
        setup.Model.Cancel();
        await running;

        var shown = Assert.Single(setup.Notifications.Published);
        Assert.Equal(_localizer.Get("Backup.Label.Cancelled"), shown.Text);
        Assert.True(setup.Model.CanAct);
    }

    [Fact]
    [Trait("spec", Copy + ": Error comprensible (Fallo inesperado)")]
    public async Task An_unexpected_failure_is_logged_technically_and_told_with_a_reference()
    {
        var setup = new Setup();
        setup.Service.Work = (_, _, _) => throw new InvalidOperationException("boom");

        await setup.Model.MakeAsync();

        Assert.Equal("MakeBackup", setup.Log.Entries.Single().Context);
        Assert.Contains("REF1", setup.Notifications.Published.Single().Text, StringComparison.Ordinal);
        Assert.True(setup.Model.CanAct);
    }

    // --- Restoring ---

    [Fact]
    [Trait("spec", Restore + ": Restauración correcta; Comparación; Aviso del cambio de contraseña; Reinicio automático")]
    public async Task Restoring_asks_the_password_shows_the_comparison_and_the_warnings_and_restarts_after_confirming()
    {
        var setup = new Setup();
        var session = new FakeSession(Result<RestorePreview>.Success(Preview(SchemaRelation.Older)));
        setup.Service.Opened = Result<IRestoreSession>.Success(session);
        setup.Forms.Script.Enqueue(TypeAndSubmit("good"));

        await setup.Model.RestoreAsync();

        var asked = Asked(setup).Single();
        Assert.True(asked.Destructive);
        Assert.Contains(_localizer.Get("Backup.Label.Counts", 1, 40, 120, 35), asked.Details![0], StringComparison.Ordinal); // what the backup holds
        Assert.Contains(_localizer.Get("Backup.Label.Counts", 2, 55, 120, 50), asked.Details[1], StringComparison.Ordinal); // what is replaced
        Assert.Contains(_localizer.Get("Backup.Label.PreviewOlder"), asked.Details);
        Assert.Contains(_localizer.Message(new Notice("Keys.RestoreAdoptsPassword")), asked.Details); // the password of the centre changes
        Assert.True(session.Restored);
        Assert.True(setup.Restarter.Restarted);
        Assert.True(session.Disposed);
        Assert.Equal("/copies", setup.Store.Saved.BackupFolder);
    }

    [Fact]
    [Trait("spec", Restore + ": Contraseña incorrecta")]
    public async Task A_wrong_password_is_told_in_the_form_and_nothing_else_is_shown_until_it_is_right()
    {
        var setup = new Setup();
        var session = new FakeSession(Result<RestorePreview>.Success(Preview()));
        setup.Service.Opened = Result<IRestoreSession>.Success(session);
        string? error = null;
        setup.Forms.Script.Enqueue(async f =>
        {
            f.Fields[0].Text = "bad";
            await f.SubmitAsync();
            error = f.Error;
            f.Cancel();
        });

        await setup.Model.RestoreAsync();

        Assert.Equal(_localizer.Message(KeyErrors.WrongCredentials), error);
        Assert.Empty(Asked(setup));
        Assert.False(session.Restored);
        Assert.True(session.Disposed);
    }

    [Fact]
    [Trait("spec", Restore + ": Fichero que no es una copia; Copia dañada")]
    public async Task A_file_that_is_not_a_backup_is_refused_with_its_reason_and_nothing_is_asked()
    {
        var setup = new Setup();
        var refusal = KeyErrors.BackupDamaged("/copies/vella.arcabackup");
        setup.Service.Opened = Result<IRestoreSession>.Failure(refusal);

        await setup.Model.RestoreAsync();

        Assert.Equal(_localizer.Message(refusal), setup.Notifications.Published.Single().Text);
        Assert.Empty(Asked(setup));
        Assert.Empty(setup.Forms.Shown);
    }

    [Fact]
    [Trait("spec", Restore + ": Copia de una versión más nueva")]
    public async Task A_backup_of_a_newer_version_is_refused_before_asking_to_confirm()
    {
        var setup = new Setup();
        var session = new FakeSession(Result<RestorePreview>.Failure(BackupErrors.VersionNewer));
        setup.Service.Opened = Result<IRestoreSession>.Success(session);
        setup.Forms.Script.Enqueue(TypeAndSubmit("good"));

        await setup.Model.RestoreAsync();

        Assert.Equal(_localizer.Message(BackupErrors.VersionNewer), setup.Notifications.Published.Single().Text);
        Assert.Empty(Asked(setup));
        Assert.False(session.Restored);
    }

    [Fact]
    [Trait("spec", Restore + ": Confirmación pendiente; Cancelar antes de sustituir")]
    public async Task Not_confirming_leaves_everything_as_it_was()
    {
        var setup = new Setup { Confirmations = new RecordingConfirmations(false) };
        var model = NewModel(setup);
        var session = new FakeSession(Result<RestorePreview>.Success(Preview()));
        setup.Service.Opened = Result<IRestoreSession>.Success(session);
        setup.Forms.Script.Enqueue(TypeAndSubmit("good"));

        await model.RestoreAsync();

        Assert.False(session.Restored);
        Assert.False(setup.Restarter.Restarted);
        Assert.True(session.Disposed);
        Assert.Empty(setup.Notifications.Published);
    }

    [Fact]
    [Trait("spec", Restore + ": Fallo al sustituir; Error comprensible")]
    public async Task A_failed_restoration_is_told_and_the_application_does_not_restart()
    {
        var setup = new Setup();
        var session = new FakeSession(Result<RestorePreview>.Success(Preview())) { Outcome = Result<RestoreOutcome>.Failure(BackupErrors.MigrationFailed) };
        setup.Service.Opened = Result<IRestoreSession>.Success(session);
        setup.Forms.Script.Enqueue(TypeAndSubmit("good"));

        await setup.Model.RestoreAsync();

        Assert.Equal(_localizer.Message(BackupErrors.MigrationFailed), setup.Notifications.Published.Single().Text);
        Assert.False(setup.Restarter.Restarted);
        Assert.False(setup.Restarter.Closed);
    }

    [Fact]
    [Trait("spec", Restore + ": No se puede reiniciar")]
    public async Task When_the_application_cannot_restart_it_says_the_data_are_restored_and_closes()
    {
        var setup = new Setup { Restarter = new FakeRestarter(false) };
        var model = NewModel(setup);
        var session = new FakeSession(Result<RestorePreview>.Success(Preview()));
        setup.Service.Opened = Result<IRestoreSession>.Success(session);
        setup.Forms.Script.Enqueue(TypeAndSubmit("good"));

        await model.RestoreAsync();

        Assert.Equal(_localizer.Get("Backup.Label.RestoredManual"), Asked(setup)[1].Consequence);
        Assert.False(setup.Restarter.Restarted);
        Assert.True(setup.Restarter.Closed);
    }

    [Fact]
    [Trait("spec", Restore + ": Doble clic")]
    public async Task While_a_restoration_is_in_progress_another_one_cannot_start()
    {
        var setup = new Setup();
        var release = new TaskCompletionSource();
        setup.Service.Opened = Result<IRestoreSession>.Failure(KeyErrors.BackupDamaged("x"));
        setup.Picker.Open = "/copies/vella.arcabackup";
        setup.Service.Work = null;
        setup.Forms.Script.Enqueue(_ => release.Task);
        var session = new FakeSession(Result<RestorePreview>.Success(Preview()));
        setup.Service.Opened = Result<IRestoreSession>.Success(session);

        var first = setup.Model.RestoreAsync();
        await Task.Delay(100);
        await setup.Model.RestoreAsync();

        Assert.Single(setup.Picker.OpenAsked);
        Assert.True(setup.Model.IsBusy);
        setup.Forms.Shown[0].Cancel();
        release.SetResult();
        await first;
    }

    // --- The block ---

    [AvaloniaFact]
    [Trait("spec", Copy + ": Bloque de Ajustes (Sin curso activo ni datos); Doble clic")]
    public async Task The_block_offers_both_actions_always_and_shows_the_progress_and_a_cancel_while_one_runs()
    {
        var setup = new Setup();
        var release = new TaskCompletionSource();
        setup.Service.Work = async (destination, steps, _) =>
        {
            steps!.Report("Backup.Stage.Copying");
            await release.Task;
            return Result<BackupResult>.Success(new BackupResult(destination, 1000));
        };
        var view = new BackupView(setup.Model);

        Assert.True(view.MakeButton.IsEnabled);
        Assert.True(view.RestoreButton.IsEnabled);
        Assert.False(view.CancelButton.IsVisible);

        var running = setup.Model.MakeAsync();
        await Task.Delay(100);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.False(view.MakeButton.IsEnabled);
        Assert.False(view.RestoreButton.IsEnabled);
        Assert.True(view.CancelButton.IsVisible);
        Assert.True(view.Progress.IsVisible);
        Assert.Equal(_localizer.Get("Backup.Stage.Copying"), view.StatusText.Text);
        release.SetResult();
        await running;
        Assert.True(view.MakeButton.IsEnabled);
        Assert.False(view.Progress.IsVisible);
    }
}
