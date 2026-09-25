// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Common;

namespace Arca.UI.Access;

/// <summary>
/// The security section of the settings (acces-i-xifrat): the reminder that without the password and the recovery key
/// there is no recovery, and the two actions, change the password and regenerate the recovery key. While one runs the
/// other cannot start and a second click does nothing.
/// </summary>
public sealed class SecurityViewModel(
    AccessFlows flows, string databasePath, INotificationService notifications, ILocalizer localizer, IErrorLog log) : ObservableObject
{
    bool _isBusy;

    public string Title => localizer.Get("Keys.Label.SecurityTitle");

    public string Note => localizer.Get("Keys.Label.SecurityNote");

    public string Warning => localizer.Get("Keys.Label.LossWarning");

    public string ChangeLabel => localizer.Get("Keys.Label.ChangeAction");

    public string RegenerateLabel => localizer.Get("Keys.Label.RegenerateAction");

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

    public Task ChangePasswordAsync() => RunAsync(flows.ChangePasswordAsync, "Keys.Label.PasswordChanged", "ChangePassword");

    public Task RegenerateKeyAsync() => RunAsync(flows.RegenerateKeyAsync, "Keys.Label.KeyRegenerated", "RegenerateRecoveryKey");

    async Task RunAsync(Func<string, CancellationToken, Task<Result<bool>?>> action, string successKey, string context)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await action(databasePath, CancellationToken.None);
            if (result is { IsSuccess: true })
            {
                notifications.Publish(NotificationKind.Success, localizer.Get(successKey));
                foreach (var notice in result.Notices)
                {
                    notifications.Publish(NotificationKind.Warning, localizer.Message(notice));
                }
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            var reference = log.LogUnexpected(e, context);
            notifications.Publish(NotificationKind.Error, localizer.Message(CommonErrors.Unexpected(reference)));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
