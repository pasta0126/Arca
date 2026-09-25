// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// The unlock stage of the start-up (acces-i-xifrat, D6): asks for the centre password until it opens the key file,
/// or until the person cancels, which stops the start so the application closes without opening the database.
/// It replaces the development key of the environment as the source of the database key.
/// </summary>
public sealed class PasswordKeyProvider(AccessService access, IUnlockPrompt prompt) : IDatabaseKeyProvider
{
    public async Task<Result<DatabaseKey>> GetKeyAsync(string databasePath, CancellationToken ct = default)
    {
        // A missing, damaged or newer key file is reported before asking for anything: no password can fix it.
        var check = access.CheckKeyFile(databasePath);
        if (!check.IsSuccess)
        {
            return Result<DatabaseKey>.Failure(check.Error!);
        }

        var failed = false;
        while (true)
        {
            var password = await prompt.AskPasswordAsync(failed, ct);
            if (password is null)
            {
                return Result<DatabaseKey>.Failure(KeyErrors.UnlockCancelled);
            }

            var unlocked = access.Unlock(databasePath, password);
            var retry = !unlocked.IsSuccess
                && (unlocked.Error!.Code == KeyErrors.WrongCredentials.Code || unlocked.Error.Code == KeyErrors.PasswordRequired.Code);
            if (!retry)
            {
                return unlocked; // opened, or a problem with the key file that another password cannot fix
            }

            failed = true;
        }
    }
}
