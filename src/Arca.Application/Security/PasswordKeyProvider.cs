// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// The unlock stage of the start-up (acces-i-xifrat, D6): it first checks that the key file can be used, because no
/// password fixes a missing or damaged one, and then hands over to the screens that ask for the password.
/// It replaces the development key of the environment as the source of the database key.
/// </summary>
public sealed class PasswordKeyProvider(AccessService access, IUnlockFlow flow) : IDatabaseKeyProvider
{
    public Task<Result<DatabaseKey>> GetKeyAsync(string databasePath, CancellationToken ct = default)
    {
        var check = access.CheckKeyFile(databasePath);
        return check.IsSuccess
            ? flow.UnlockAsync(databasePath, ct)
            : Task.FromResult(Result<DatabaseKey>.Failure(check.Error!));
    }
}
