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

    /// <summary>Whether the key file next to the database can be used. Reads only; nothing is modified.</summary>
    public Result<KeyFile> CheckKeyFile(string databasePath) => store.Read(databasePath);

    /// <summary>
    /// Whether the recovery key typed opens the key file, before asking for anything more. It says only yes or no,
    /// as a wrong password does, and keeps nothing.
    /// </summary>
    public Result<bool> CheckRecoveryKey(string databasePath, string? typedRecoveryKey)
    {
        var read = store.Read(databasePath);
        if (!read.IsSuccess)
        {
            return Result<bool>.Failure(read.Error!);
        }

        var recovery = RecoveryKey.Normalize(typedRecoveryKey);
        if (!recovery.IsSuccess)
        {
            return Result<bool>.Failure(recovery.Error!);
        }

        var opened = KeyWrapping.UnwrapWithRecoveryKey(crypto, read.Value!, recovery.Value!);
        if (!opened.IsSuccess)
        {
            return Result<bool>.Failure(opened.Error!);
        }

        opened.Value!.Dispose();
        return Result<bool>.Success(true);
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
    /// Forgotten password: the recovery key opens the data, and the person must set a new password and a new recovery
    /// key (which replaces the old one). Nothing is saved until the new key is confirmed with <see cref="Commit"/>.
    /// </summary>
    public Result<PendingKeyChange> PrepareReset(string databasePath, string? typedRecoveryKey, string? newPassword, string? confirmation)
    {
        var read = store.Read(databasePath);
        if (!read.IsSuccess)
        {
            return Result<PendingKeyChange>.Failure(read.Error!);
        }

        var recovery = RecoveryKey.Normalize(typedRecoveryKey);
        if (!recovery.IsSuccess)
        {
            return Result<PendingKeyChange>.Failure(recovery.Error!);
        }

        var opened = KeyWrapping.UnwrapWithRecoveryKey(crypto, read.Value!, recovery.Value!);
        if (!opened.IsSuccess)
        {
            return Result<PendingKeyChange>.Failure(opened.Error!);
        }

        var key = opened.Value!;
        var accepted = CheckNewPassword(newPassword, confirmation);
        if (!accepted.IsSuccess)
        {
            key.Dispose();
            return Result<PendingKeyChange>.Failure(accepted.Error!);
        }

        var bytes = PasswordText.ToBytes(newPassword!);
        try
        {
            var newRecovery = RecoveryKey.Generate();
            var file = KeyWrapping.ReplacePassword(crypto, read.Value!, key, bytes, _cost);
            file = KeyWrapping.ReplaceRecovery(crypto, file, key, newRecovery);
            return Result<PendingKeyChange>.Success(new PendingKeyChange(key, newRecovery, file), [.. accepted.Notices]);
        }
        catch
        {
            key.Dispose();
            throw;
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
        }
    }

    /// <summary>
    /// From settings: a new recovery key that will replace the current one. It asks for the current password so
    /// that whoever finds the computer unlocked cannot take over the recovery. Nothing is saved until
    /// <see cref="Commit"/>, so cancelling keeps the previous key valid.
    /// </summary>
    public Result<PendingKeyChange> PrepareRegeneration(string databasePath, string? password)
    {
        var read = store.Read(databasePath);
        if (!read.IsSuccess)
        {
            return Result<PendingKeyChange>.Failure(read.Error!);
        }

        var opened = UnwrapWithPassword(read.Value!, password);
        if (!opened.IsSuccess)
        {
            return Result<PendingKeyChange>.Failure(opened.Error!);
        }

        var key = opened.Value!;
        try
        {
            var newRecovery = RecoveryKey.Generate();
            var file = KeyWrapping.ReplaceRecovery(crypto, read.Value!, key, newRecovery);
            return Result<PendingKeyChange>.Success(new PendingKeyChange(key, newRecovery, file));
        }
        catch
        {
            key.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Saves a prepared change once the person has typed the groups asked. A wrong or missing confirmation saves
    /// nothing and the person can look at the key again.
    /// </summary>
    public Result<bool> Commit(string databasePath, PendingKeyChange pending, IReadOnlyList<string?>? typedGroups)
    {
        var mistake = pending.Challenge.Verify(typedGroups);
        if (mistake is not null)
        {
            return Result<bool>.Failure(mistake);
        }

        try
        {
            // The recovery wrapper is the one that always changes here; a reset also changes the password wrapper, which
            // the read-back comparison covers.
            var saved = WriteVerified(databasePath, pending.NewFile, file =>
                KeyWrapping.UnwrapWithRecoveryKey(crypto, file, pending.RecoveryKey).IsSuccess);
            return saved ? Result<bool>.Success(true) : Result<bool>.Failure(KeyErrors.ChangeFailed);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Result<bool>.Failure(KeyErrors.ChangeFailed);
        }
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
            var saved = WriteVerified(databasePath, replaced, file => KeyWrapping.UnwrapWithPassword(crypto, file, bytes).IsSuccess);
            return saved
                ? Result<bool>.Success(true, [.. accepted.Notices, .. notices])
                : Result<bool>.Failure(KeyErrors.ChangeFailed);
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

    /// <summary>
    /// Saves a new key file and proves it (acces-i-xifrat, D3): it is read back, must be the file that was written, and
    /// must open with the new credentials. Only then is the previous version removed, so a replaced password or recovery
    /// key stops opening the data at once. If the proof fails the previous file is put back and the change is refused.
    /// </summary>
    bool WriteVerified(string databasePath, KeyFile newFile, Func<KeyFile, bool> opensWithNewCredentials)
    {
        var before = store.Read(databasePath);
        store.Write(databasePath, newFile);
        var readBack = store.Read(databasePath);
        if (readBack.IsSuccess && SameWrappers(readBack.Value!, newFile) && opensWithNewCredentials(readBack.Value!))
        {
            DiscardPreviousQuietly(databasePath);
            return true;
        }

        if (before.IsSuccess)
        {
            store.Write(databasePath, before.Value!);
            var restored = store.Read(databasePath);
            if (restored.IsSuccess && SameWrappers(restored.Value!, before.Value!))
            {
                DiscardPreviousQuietly(databasePath);
            }

            // Otherwise the previous version stays next to the file: it is the only good copy left.
        }

        return false;
    }

    static bool SameWrappers(KeyFile a, KeyFile b) =>
        a.FormatVersion == b.FormatVersion
        && a.Password.Parameters == b.Password.Parameters
        && a.Password.Salt.AsSpan().SequenceEqual(b.Password.Salt)
        && a.Password.Wrapped.AsSpan().SequenceEqual(b.Password.Wrapped)
        && a.Recovery.Salt.AsSpan().SequenceEqual(b.Recovery.Salt)
        && a.Recovery.Wrapped.AsSpan().SequenceEqual(b.Recovery.Wrapped);

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
