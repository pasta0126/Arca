// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// DEVELOPMENT ONLY, until acces-i-xifrat provides the real access flow: reads a 64-character hexadecimal key
/// from the environment. No key is stored in code; without the variable the application cannot open a database.
/// </summary>
public sealed class EnvironmentKeyProvider(Func<string, string?>? readVariable = null) : IDatabaseKeyProvider
{
    public const string VariableName = "ARCA_DEV_DB_KEY";

    readonly Func<string, string?> _read = readVariable ?? Environment.GetEnvironmentVariable;

    public Task<Result<DatabaseKey>> GetKeyAsync(string databasePath, CancellationToken ct = default)
    {
        var text = _read(VariableName);
        if (string.IsNullOrWhiteSpace(text) || text.Length != DatabaseKey.Length * 2)
        {
            return Task.FromResult(Result<DatabaseKey>.Failure(StorageErrors.KeyNotAvailable));
        }

        try
        {
            return Task.FromResult(Result<DatabaseKey>.Success(new DatabaseKey(Convert.FromHexString(text))));
        }
        catch (FormatException)
        {
            return Task.FromResult(Result<DatabaseKey>.Failure(StorageErrors.KeyNotAvailable));
        }
    }
}
