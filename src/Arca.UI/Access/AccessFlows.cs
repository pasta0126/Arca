// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Security;
using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.UI.Access;

/// <summary>
/// The screens of the centre password, in the order the specs describe (acces-i-xifrat): unlocking at start-up with the
/// way out through the recovery key, and the first run that creates the password, shows and confirms the recovery key
/// and creates the database. The checks run off the screen thread and never put a password in a message or the log.
/// </summary>
/// <param name="createDatabase">Creates the database and its key file as one operation (Infrastructure provides it).</param>
public sealed class AccessFlows(
    AccessService access,
    IFormPresenter presenter,
    ILocalizer localizer,
    Func<string, NewAccess, IReadOnlyList<string?>, CancellationToken, Task<Result<bool>>> createDatabase) : IUnlockFlow, IFirstRunFlow
{
    const int Minimum = PasswordPolicy.MinimumLength;

    public async Task<Result<DatabaseKey>> UnlockAsync(string databasePath, CancellationToken ct)
    {
        while (true)
        {
            DatabaseKey? key = null;
            Error? fatal = null;
            var password = new FormField(Text("Keys.Label.PasswordField"), isSecret: true);
            var form = NewForm("Keys.Label.UnlockTitle", "Keys.Label.UnlockIntro", "Keys.Label.UnlockButton", "Keys.Label.Checking", [password],
                async f =>
                {
                    var typed = password.Text;
                    var result = await Task.Run(() => access.Unlock(databasePath, typed), ct);
                    if (result.IsSuccess)
                    {
                        key = result.Value;
                        return true;
                    }

                    if (result.Error!.Code is "Keys.WrongCredentials" or "Keys.PasswordRequired")
                    {
                        f.Error = localizer.Message(result.Error);
                        password.Text = string.Empty;
                        return false;
                    }

                    fatal = result.Error; // a problem with the key file: another password cannot fix it
                    return true;
                },
                secondary: "Keys.Label.ForgotButton");

            switch (await presenter.ShowAsync(form, ct))
            {
                case FormOutcome.Submitted:
                    return fatal is null ? Result<DatabaseKey>.Success(key!) : Result<DatabaseKey>.Failure(fatal);
                case FormOutcome.Secondary:
                    var recovered = await RecoverAsync(databasePath, ct);
                    if (recovered is not null)
                    {
                        return recovered;
                    }

                    break; // went back: ask for the password again
                default:
                    return Result<DatabaseKey>.Failure(KeyErrors.UnlockCancelled);
            }
        }
    }

    public async Task<Result<DatabaseKey>> CreateAsync(string databasePath, CancellationToken ct)
    {
        NewAccess? created = null;
        try
        {
            var passwords = await AskNewPasswordAsync(
                "Keys.Label.CreateTitle", "Keys.Label.CreateIntro", "Keys.Label.ContinueButton",
                (password, confirmation) =>
                {
                    var result = access.CreateAccess(password, confirmation);
                    created = result.Value;
                    return result.IsSuccess ? null : result.Error;
                },
                ct);
            if (!passwords || created is null)
            {
                return Result<DatabaseKey>.Failure(KeyErrors.UnlockCancelled);
            }

            var confirmed = await ShowKeyAsync(
                created.RecoveryKey, created.Challenge, "Keys.Label.CreateButton", "Keys.Label.Creating",
                async (groups, token) =>
                {
                    var made = await createDatabase(databasePath, created, groups, token);
                    return made.IsSuccess ? null : made.Error;
                },
                ct);
            return confirmed
                ? Result<DatabaseKey>.Success(new DatabaseKey(created.DataKey.ToArray()))
                : Result<DatabaseKey>.Failure(KeyErrors.UnlockCancelled);
        }
        finally
        {
            created?.Dispose();
        }
    }

    /// <summary>
    /// From settings: the current password and a new one. Returns null if the person gave up, or the result with its
    /// notices (which remind that earlier copies keep the old password).
    /// </summary>
    public async Task<Result<bool>?> ChangePasswordAsync(string databasePath, CancellationToken ct)
    {
        Result<bool>? outcome = null;
        var current = new FormField(Text("Keys.Label.CurrentField"), isSecret: true);
        var password = new FormField(Text("Keys.Label.NewPasswordField"), isSecret: true);
        var confirmation = new FormField(Text("Keys.Label.ConfirmationField"), isSecret: true);
        var form = NewForm("Keys.Label.ChangeTitle", "Keys.Label.ChangeIntro", "Keys.Label.ChangeButton", "Keys.Label.Saving",
            [current, password, confirmation],
            async f =>
            {
                var (now, next, again) = (current.Text, password.Text, confirmation.Text);
                var result = await Task.Run(() => access.ChangePassword(databasePath, now, next, again), ct);
                if (result.IsSuccess)
                {
                    outcome = result;
                    return true;
                }

                f.Error = localizer.Message(result.Error!);
                return false;
            },
            hints: texts => PasswordHints(texts[1]),
            warning: "Keys.Label.LossWarning");
        return await presenter.ShowAsync(form, ct) == FormOutcome.Submitted ? outcome : null;
    }

    /// <summary>
    /// From settings: a new recovery key after the current password. The old key stays valid until the new one is
    /// confirmed, so giving up at any point changes nothing. Returns null if the person gave up.
    /// </summary>
    public async Task<Result<bool>?> RegenerateKeyAsync(string databasePath, CancellationToken ct)
    {
        PendingKeyChange? pending = null;
        try
        {
            var current = new FormField(Text("Keys.Label.CurrentField"), isSecret: true);
            var first = NewForm("Keys.Label.RegenerateTitle", "Keys.Label.RegenerateIntro", "Keys.Label.ContinueButton", "Keys.Label.Checking",
                [current],
                async f =>
                {
                    var typed = current.Text;
                    var result = await Task.Run(() => access.PrepareRegeneration(databasePath, typed), ct);
                    if (result.IsSuccess)
                    {
                        pending = result.Value;
                        return true;
                    }

                    f.Error = localizer.Message(result.Error!);
                    return false;
                });
            if (await presenter.ShowAsync(first, ct) != FormOutcome.Submitted || pending is null)
            {
                return null;
            }

            var confirmed = await ShowKeyAsync(
                pending.RecoveryKey, pending.Challenge, "Keys.Label.ConfirmButton", "Keys.Label.Saving",
                (groups, _) =>
                {
                    var saved = access.Commit(databasePath, pending, groups);
                    return Task.FromResult(saved.IsSuccess ? null : saved.Error);
                },
                ct);
            return confirmed ? Result<bool>.Success(true) : null;
        }
        finally
        {
            pending?.Dispose();
        }
    }

    /// <summary>
    /// Forgotten password: the recovery key, then a new password, then the new recovery key shown and confirmed.
    /// Returns the key that opens the data, or null when the person went back without finishing (nothing changes).
    /// </summary>
    async Task<Result<DatabaseKey>?> RecoverAsync(string databasePath, CancellationToken ct)
    {
        string? typedKey = null;
        var keyField = new FormField(Text("Keys.Label.RecoveryField"), isSecret: false);
        var first = NewForm("Keys.Label.RecoveryTitle", "Keys.Label.RecoveryIntro", "Keys.Label.ContinueButton", "Keys.Label.Checking", [keyField],
            async f =>
            {
                var typed = keyField.Text;
                var result = await Task.Run(() => access.CheckRecoveryKey(databasePath, typed), ct);
                if (result.IsSuccess)
                {
                    typedKey = typed;
                    return true;
                }

                f.Error = localizer.Message(result.Error!);
                return false;
            });
        if (await presenter.ShowAsync(first, ct) != FormOutcome.Submitted)
        {
            return null;
        }

        PendingKeyChange? pending = null;
        try
        {
            var chosen = await AskNewPasswordAsync(
                "Keys.Label.ResetTitle", "Keys.Label.ResetIntro", "Keys.Label.ContinueButton",
                (password, confirmation) =>
                {
                    pending?.Dispose();
                    var result = access.PrepareReset(databasePath, typedKey, password, confirmation);
                    pending = result.Value;
                    return result.IsSuccess ? null : result.Error;
                },
                ct);
            if (!chosen || pending is null)
            {
                return null;
            }

            var confirmed = await ShowKeyAsync(
                pending.RecoveryKey, pending.Challenge, "Keys.Label.ConfirmButton", "Keys.Label.Saving",
                (groups, _) =>
                {
                    var saved = access.Commit(databasePath, pending, groups);
                    return Task.FromResult(saved.IsSuccess ? null : saved.Error);
                },
                ct);
            return confirmed ? Result<DatabaseKey>.Success(new DatabaseKey(pending.DataKey.ToArray())) : null;
        }
        finally
        {
            pending?.Dispose();
        }
    }

    /// <summary>The form of a new password with its confirmation, the length counter and the strength indicator.</summary>
    async Task<bool> AskNewPasswordAsync(
        string titleKey, string introKey, string buttonKey, Func<string, string, Error?> apply, CancellationToken ct)
    {
        var password = new FormField(Text("Keys.Label.NewPasswordField"), isSecret: true);
        var confirmation = new FormField(Text("Keys.Label.ConfirmationField"), isSecret: true);
        var form = NewForm(titleKey, introKey, buttonKey, "Keys.Label.Checking", [password, confirmation],
            async f =>
            {
                var (typed, again) = (password.Text, confirmation.Text);
                var error = await Task.Run(() => apply(typed, again), ct);
                if (error is null)
                {
                    return true;
                }

                f.Error = localizer.Message(error);
                return false;
            },
            hints: texts => PasswordHints(texts[0]),
            warning: "Keys.Label.LossWarning");
        return await presenter.ShowAsync(form, ct) == FormOutcome.Submitted;
    }

    /// <summary>Shows the recovery key once and asks for the groups that prove it was written down.</summary>
    async Task<bool> ShowKeyAsync(
        string recoveryKey, RecoveryKeyChallenge challenge, string buttonKey, string busyKey,
        Func<IReadOnlyList<string?>, CancellationToken, Task<Error?>> confirm, CancellationToken ct)
    {
        var fields = challenge.Indices.Select(i => new FormField(Text("Keys.Label.GroupField", i + 1), isSecret: false)).ToList();
        var formatted = RecoveryKey.Format(recoveryKey);
        var form = new AccessFormViewModel(
            Text("Keys.Label.KeyTitle"), Text("Keys.Label.KeyIntro"), Text(buttonKey), Text("Common.Label.Cancel"), Text(busyKey), fields,
            async f =>
            {
                var groups = fields.Select(field => (string?)field.Text).ToList();
                var error = await confirm(groups, ct);
                if (error is null)
                {
                    return true;
                }

                f.Error = localizer.Message(error);
                return false;
            })
        {
            Warning = Text("Keys.Label.LossWarning"),
            Secret = new SecretDisplay(
                formatted, Text("Keys.Label.KeyCaption"), Text("Keys.Label.CopyButton"), Text("Keys.Label.PrintButton"),
                Text("Keys.Label.KeyCopied"), Text("Keys.Label.KeyPrinted"), Text("Keys.Label.PrintTitle"),
                [Text("Keys.Label.PrintIntro"), formatted, Text("Keys.Label.PrintAdvice")]),
        };
        return await presenter.ShowAsync(form, ct) == FormOutcome.Submitted;
    }

    List<string> PasswordHints(string password)
    {
        var lines = new List<string> { Text("Keys.Label.LengthCounter", PasswordText.Length(password), Minimum) };
        var check = PasswordPolicy.Check(password);
        if (check.IsSuccess)
        {
            lines.Add(Text(check.Value!.Strength switch
            {
                PasswordStrength.Weak => "Keys.Label.StrengthWeak",
                PasswordStrength.Fair => "Keys.Label.StrengthFair",
                _ => "Keys.Label.StrengthGood",
            }));
            lines.AddRange(check.Notices.Select(localizer.Message));
        }
        else if (check.Error!.Code == KeyErrors.PasswordTooCommon.Code)
        {
            lines.Add(localizer.Message(check.Error));
        }

        return lines;
    }

    AccessFormViewModel NewForm(
        string titleKey, string introKey, string primaryKey, string busyKey, IReadOnlyList<FormField> fields,
        Func<AccessFormViewModel, Task<bool>> submit, string? secondary = null,
        Func<IReadOnlyList<string>, IReadOnlyList<string>>? hints = null, string? warning = null) =>
        new(Text(titleKey), Text(introKey), Text(primaryKey), Text("Common.Label.Cancel"), Text(busyKey), fields, submit, hints)
        {
            SecondaryLabel = secondary is null ? string.Empty : Text(secondary),
            Warning = warning is null ? string.Empty : Text(warning),
        };

    string Text(string key, params object[] args) => localizer.Get(key, args);
}
