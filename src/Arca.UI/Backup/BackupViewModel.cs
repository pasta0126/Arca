// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Backup;
using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Security;
using Arca.Domain.Common;
using Arca.UI.Access;
using Arca.UI.Common;
using Arca.UI.Notifications;
using Arca.UI.Preferences;

namespace Arca.UI.Backup;

/// <summary>
/// The «Còpia de seguretat» block of Settings (copies-de-seguretat): making a backup and restoring one. Each action asks what it has to, tells its
/// stages while it works, says how it ended and is protected against a second run; the whole flow, dialogs included, counts as running.
/// </summary>
public sealed class BackupViewModel(
    IBackupService backups, IBackupFilePicker picker, IConfirmationService confirmations, AccessFlows flows, INotificationService notifications,
    IApplicationRestarter restarter, UiPreferencesSession preferences, IClock clock, ILocalizer localizer, IErrorLog log) : ObservableObject
{
    bool _isBusy;
    string _status = string.Empty;
    Action? _cancel;

    public string Title => localizer.Get("Backup.Label.Title");

    public string Note => localizer.Get("Backup.Label.Note");

    public string MakeLabel => localizer.Get("Backup.Label.MakeButton");

    public string RestoreLabel => localizer.Get("Backup.Label.RestoreButton");

    public string CancelLabel => localizer.Get("Common.Label.Cancel");

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
            {
                Raise(nameof(CanAct));
            }
        }
    }

    public bool CanAct => !IsBusy;

    /// <summary>The stage it is in while it works, empty when it does nothing.</summary>
    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    /// <summary>Gives up what is running. Only has an effect until the data are replaced.</summary>
    public void Cancel() => _cancel?.Invoke();

    public Task MakeAsync() => RunAsync("MakeBackup", MakeCoreAsync);

    public Task RestoreAsync() => RunAsync("RestoreBackup", RestoreCoreAsync);

    async Task RunAsync(string context, Func<CancellationToken, Task> flow)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        using var source = new CancellationTokenSource();
        _cancel = source.Cancel;
        try
        {
            await flow(source.Token);
        }
        catch (OperationCanceledException)
        {
            notifications.Publish(NotificationKind.Warning, localizer.Get("Backup.Label.Cancelled"));
        }
        catch (Exception e)
        {
            var reference = log.LogUnexpected(e, context);
            notifications.Publish(NotificationKind.Error, localizer.Message(CommonErrors.Unexpected(reference)));
        }
        finally
        {
            Status = string.Empty;
            _cancel = null;
            IsBusy = false;
        }
    }

    // --- Making a backup ---

    async Task MakeCoreAsync(CancellationToken ct)
    {
        var warned = await confirmations.ConfirmAsync(
            new ConfirmationRequest(
                localizer.Get("Backup.Label.MakeTitle"), localizer.Get("Backup.Label.MinorsWarning"), localizer.Get("Backup.Label.MakeConfirm")), ct);
        if (!warned)
        {
            return;
        }

        var destination = await picker.PickSaveAsync(backups.SuggestedName(clock.UtcNow), preferences.BackupFolder);
        if (destination is null)
        {
            return;
        }

        if (backups.Exists(destination))
        {
            var overwrite = await confirmations.ConfirmAsync(
                new ConfirmationRequest(
                    localizer.Get("Backup.Label.OverwriteTitle"), localizer.Get("Backup.Label.OverwriteConsequence", System.IO.Path.GetFileName(destination)),
                    localizer.Get("Backup.Label.OverwriteConfirm"), Destructive: true), ct);
            if (!overwrite)
            {
                return;
            }
        }

        preferences.SetBackupFolder(System.IO.Path.GetDirectoryName(destination));
        var made = await backups.BackUpAsync(destination, StageReporter(), ct);
        if (made.IsSuccess)
        {
            notifications.Publish(NotificationKind.Success, localizer.Get("Backup.Label.Done", made.Value!.Path, SizeText(made.Value.SizeBytes)));
        }
        else
        {
            notifications.Publish(NotificationKind.Error, localizer.Message(made.Error!));
        }
    }

    // --- Restoring a backup ---

    async Task RestoreCoreAsync(CancellationToken ct)
    {
        var file = await picker.PickOpenAsync(preferences.BackupFolder);
        if (file is null)
        {
            return;
        }

        var opened = backups.Open(file);
        if (!opened.IsSuccess)
        {
            notifications.Publish(NotificationKind.Error, localizer.Message(opened.Error!));
            return;
        }

        using var session = opened.Value!;
        var unlocked = await UnlockAsync(session, ct);
        if (unlocked is null)
        {
            return;
        }

        if (unlocked.IsSuccess is false)
        {
            notifications.Publish(NotificationKind.Error, localizer.Message(unlocked.Error!));
            return;
        }

        var preview = await session.PreviewAsync(ct);
        if (!preview.IsSuccess)
        {
            notifications.Publish(NotificationKind.Error, localizer.Message(preview.Error!));
            return;
        }

        var details = PreviewLines(preview.Value!, unlocked.Notices);
        var confirmed = await confirmations.ConfirmAsync(
            new ConfirmationRequest(
                localizer.Get("Backup.Label.RestoreTitle"), localizer.Get("Backup.Label.RestoreConsequence"), localizer.Get("Backup.Label.RestoreConfirm"),
                Destructive: true, Details: details), ct);
        if (!confirmed)
        {
            return;
        }

        preferences.SetBackupFolder(System.IO.Path.GetDirectoryName(file));
        var restored = await session.RestoreAsync(StageReporter(), ct);
        if (!restored.IsSuccess)
        {
            notifications.Publish(NotificationKind.Error, localizer.Message(restored.Error!));
            return;
        }

        Status = localizer.Get("Backup.Stage.Restarting");
        if (restarter.CanRestart)
        {
            notifications.Publish(NotificationKind.Success, localizer.Get("Backup.Label.Restored"));
            restarter.Restart();
            return;
        }

        // The data are restored but this installation cannot launch itself again: say so, and close so nothing of the old data stays on screen.
        await confirmations.ConfirmAsync(
            new ConfirmationRequest(localizer.Get("Backup.Label.RestoredTitle"), localizer.Get("Backup.Label.RestoredManual"), localizer.Get("Backup.Label.RestoredClose")), CancellationToken.None);
        restarter.Close();
    }

    /// <summary>The password of the backup, or its recovery key when it is forgotten. Null when the person gave up.</summary>
    async Task<Result<bool>?> UnlockAsync(IRestoreSession session, CancellationToken ct)
    {
        while (true)
        {
            Result<bool>? unlocked = null;
            var password = new FormField(localizer.Get("Keys.Label.PasswordField"), isSecret: true);
            var form = flows.NewForm("Backup.Label.UnlockTitle", "Backup.Label.UnlockIntro", "Keys.Label.ContinueButton", "Keys.Label.Checking", [password],
                async f =>
                {
                    var typed = password.Text;
                    var result = await session.UnlockWithPasswordAsync(typed, ct);
                    if (result.IsSuccess)
                    {
                        unlocked = result;
                        return true;
                    }

                    f.Error = localizer.Message(result.Error!);
                    password.Text = string.Empty;
                    return false;
                },
                secondary: "Keys.Label.ForgotButton");
            switch (await flows.Presenter.ShowAsync(form, ct))
            {
                case FormOutcome.Submitted:
                    return unlocked;
                case FormOutcome.Secondary:
                    var recovered = await RecoverAsync(session, ct);
                    if (recovered is not null)
                    {
                        return recovered;
                    }

                    break; // went back: ask for the password again
                default:
                    return null;
            }
        }
    }

    async Task<Result<bool>?> RecoverAsync(IRestoreSession session, CancellationToken ct)
    {
        string? typedKey = null;
        var keyField = new FormField(localizer.Get("Keys.Label.RecoveryField"), isSecret: false);
        var first = flows.NewForm("Keys.Label.RecoveryTitle", "Backup.Label.RecoveryIntro", "Keys.Label.ContinueButton", "Keys.Label.Checking", [keyField],
            f =>
            {
                var result = session.CheckRecoveryKey(keyField.Text);
                if (result.IsSuccess)
                {
                    typedKey = keyField.Text;
                    return Task.FromResult(true);
                }

                f.Error = localizer.Message(result.Error!);
                return Task.FromResult(false);
            });
        if (await flows.Presenter.ShowAsync(first, ct) != FormOutcome.Submitted)
        {
            return null;
        }

        PendingKeyChange? pending = null;
        try
        {
            var chosen = await flows.AskNewPasswordAsync(
                "Keys.Label.ResetTitle", "Keys.Label.ResetIntro", "Keys.Label.ContinueButton",
                (password, confirmation) =>
                {
                    pending?.Dispose();
                    var result = session.BeginRecovery(typedKey, password, confirmation);
                    pending = result.Value;
                    return result.IsSuccess ? null : result.Error;
                },
                ct);
            if (!chosen || pending is null)
            {
                return null;
            }

            Result<bool>? adopted = null;
            var confirmed = await flows.ShowKeyAsync(
                pending.RecoveryKey, pending.Challenge, "Keys.Label.ConfirmButton", "Keys.Label.Saving",
                async (groups, token) =>
                {
                    var result = await session.ConfirmRecoveryAsync(pending, groups, token);
                    adopted = result.IsSuccess ? result : null;
                    return result.IsSuccess ? null : result.Error;
                },
                ct);
            return confirmed ? adopted : null;
        }
        finally
        {
            pending?.Dispose();
        }
    }

    List<string> PreviewLines(RestorePreview preview, IReadOnlyList<Notice> notices)
    {
        var lines = new List<string>
        {
            localizer.Get("Backup.Label.PreviewBackup", CountsText(preview.Backup)),
            localizer.Get("Backup.Label.PreviewCurrent", CountsText(preview.Current)),
            localizer.Get("Backup.Label.PreviewLosses"),
        };
        if (preview.Relation == SchemaRelation.Older)
        {
            lines.Add(localizer.Get("Backup.Label.PreviewOlder"));
        }

        lines.AddRange(notices.Select(localizer.Message)); // that the password of the centre will be the one of the backup
        return lines;
    }

    string CountsText(ContentCounts counts) =>
        localizer.Get("Backup.Label.Counts", counts.Years, counts.Students, counts.Lockers, counts.Assignments);

    string SizeText(long bytes) => bytes >= 1_000_000
        ? localizer.Get("Backup.Label.SizeMb", (bytes / 1_000_000.0).ToString("0.0", System.Globalization.CultureInfo.CurrentCulture))
        : localizer.Get("Backup.Label.SizeKb", Math.Max(1, bytes / 1000));

    Progress<string> StageReporter() => new Progress<string>(key => Status = localizer.Get(key));
}
