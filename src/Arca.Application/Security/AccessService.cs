// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// The use cases of the centre password (acces-i-xifrat, D1, D5 and D9): create it, unlock with it, change it and
/// reset it with the recovery key. The password arrives as text from the screen and lives as bytes only while it is
/// derived; those bytes are wiped when done. Nothing here logs, stores or puts a password into an error.
/// </summary>
public sealed class AccessService(IKeyCrypto crypto, IKeyFileStore store, Argon2Parameters? cost = null)
{
    readonly Argon2Parameters _cost = cost ?? Argon2Parameters.Default;

    /// <summary>
    /// First run: checks the password and its confirmation, and makes the database key, the recovery key and the key
    /// file. It writes nothing: creating the database and the key file is one atomic operation (group 4).
    /// </summary>
    public Result<NewAccess> CreateAccess(string? password, string? confirmation)
    {
        var accepted = CheckNewPassword(password, confirmation);
        if (!accepted.IsSuccess)
        {
            return Result<NewAccess>.Failure(accepted.Error!);
        }

        var dataKey = crypto.GenerateDataKey();
        var recoveryKey = RecoveryKey.Generate();
        var bytes = PasswordText.ToBytes(password!);
        try
        {
            var file = KeyWrapping.Create(crypto, dataKey, bytes, recoveryKey, _cost);
            return Result<NewAccess>.Success(new NewAccess(dataKey, recoveryKey, file), [.. accepted.Notices]);
        }
        catch
        {
            dataKey.Dispose();
            throw;
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        }
    }

    /// <summary>
    /// Opens the database key with the password. A later successful unlock is what lets the previous key file go
    /// (D3). A wrong password says only that, so nothing is revealed.
    /// </summary>
    public Result<DatabaseKey> Unlock(string databasePath, string? password)
    {
        var read = store.Read(databasePath);
        if (!read.IsSuccess)
        {
            return Result<DatabaseKey>.Failure(read.Error!);
        }

        var unlocked = UnwrapWithPassword(read.Value!, password);
        if (unlocked.IsSuccess)
        {
            DiscardPreviousQuietly(databasePath);
        }

        return unlocked;
    }

    /// <summary>
    /// Changes the password without touching the database: only the password wrapper is rebuilt, and the file is
    /// replaced atomically, so a failure leaves the old password working. Copies made before keep the old password.
    /// </summary>
    public Result<bool> ChangePassword(string databasePath, string? current, string? newPassword, string? confirmation)
    {
        var read = store.Read(databasePath);
        if (!read.IsSuccess)
        {
            return Result<bool>.Failure(read.Error!);
        }

        var opened = UnwrapWithPassword(read.Value!, current);
        if (!opened.IsSuccess)
        {
            return Result<bool>.Failure(opened.Error!);
        }

        using var key = opened.Value!;
        return Rewrite(databasePath, read.Value!, key, newPassword, confirmation, new Notice("Keys.BackupsKeepOldPassword"));
    }

    /// <summary>
    /// Forgotten password: the recovery key opens the data and the person sets a new password. The application is then
    /// unlocked, so the database key is returned for it to continue.
    /// </summary>
    public Result<DatabaseKey> ResetPassword(string databasePath, string? typedRecoveryKey, string? newPassword, string? confirmation)
    {
        var read = store.Read(databasePath);
        if (!read.IsSuccess)
        {
            return Result<DatabaseKey>.Failure(read.Error!);
        }

        var recovery = RecoveryKey.Normalize(typedRecoveryKey);
        if (!recovery.IsSuccess)
        {
            return Result<DatabaseKey>.Failure(recovery.Error!);
        }

        var opened = KeyWrapping.UnwrapWithRecoveryKey(crypto, read.Value!, recovery.Value!);
        if (!opened.IsSuccess)
        {
            return opened;
        }

        var key = opened.Value!;
        var rewritten = Rewrite(databasePath, read.Value!, key, newPassword, confirmation);
        if (rewritten.IsSuccess)
        {
            return Result<DatabaseKey>.Success(key, [.. rewritten.Notices]);
        }

        key.Dispose();
        return Result<DatabaseKey>.Failure(rewritten.Error!);
    }

    Result<bool> Rewrite(
        string databasePath, KeyFile current, DatabaseKey key, string? newPassword, string? confirmation, params Notice[] notices)
    {
        var accepted = CheckNewPassword(newPassword, confirmation);
        if (!accepted.IsSuccess)
        {
            return Result<bool>.Failure(accepted.Error!);
        }

        var bytes = PasswordText.ToBytes(newPassword!);
        try
        {
            var replaced = KeyWrapping.ReplacePassword(crypto, current, key, bytes, _cost);
            store.Write(databasePath, replaced);
            return Result<bool>.Success(true, [.. accepted.Notices, .. notices]);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Result<bool>.Failure(KeyErrors.ChangeFailed);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        }
    }

    Result<DatabaseKey> UnwrapWithPassword(KeyFile file, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return Result<DatabaseKey>.Failure(KeyErrors.PasswordRequired);
        }

        var bytes = PasswordText.ToBytes(password);
        try
        {
            return KeyWrapping.UnwrapWithPassword(crypto, file, bytes);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        }
    }

    static Result<PasswordAssessment> CheckNewPassword(string? password, string? confirmation)
    {
        var assessment = PasswordPolicy.Check(password);
        if (!assessment.IsSuccess)
        {
            return assessment;
        }

        return string.Equals(PasswordText.Normalize(password!), PasswordText.Normalize(confirmation ?? string.Empty), StringComparison.Ordinal)
            ? assessment
            : Result<PasswordAssessment>.Failure(KeyErrors.PasswordMismatch);
    }

    void DiscardPreviousQuietly(string databasePath)
    {
        try
        {
            store.DiscardPrevious(databasePath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Keeping an old key file a little longer is harmless; failing an unlock over it would not be.
        }
    }
}
