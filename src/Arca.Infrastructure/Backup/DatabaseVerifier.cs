// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Infrastructure.Storage;
using Microsoft.Data.Sqlite;

namespace Arca.Infrastructure.Backup;

/// <summary>Checks a database file with a key without changing it: it opens, passes the integrity check and is ARCA's.</summary>
public static class DatabaseVerifier
{
    const int SqliteCorrupt = 11;
    const int SqliteNotADatabase = 26;

    /// <returns>Null when the file is sound; otherwise the error that says why not.</returns>
    public static async Task<Error?> VerifyAsync(string path, DatabaseKey key, CancellationToken ct = default)
    {
        try
        {
            await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
            await connection.OpenAsync(ct);
            SqlCipherKeyInterceptor.Unlock(connection, key);

            var integrity = await ScalarAsync(connection, "PRAGMA integrity_check", ct);
            if (!string.Equals(integrity, "ok", StringComparison.Ordinal))
            {
                return StorageErrors.Unreadable(path);
            }

            var id = await ScalarAsync(connection, "PRAGMA application_id", ct);
            return id == ArcaDatabase.ApplicationId.ToString(CultureInfo.InvariantCulture) ? null : StorageErrors.NotArcaDatabase(path);
        }
        catch (SqliteException e) when (e.SqliteErrorCode is SqliteNotADatabase or SqliteCorrupt)
        {
            return StorageErrors.Unreadable(path);
        }
    }

    static async Task<string?> ScalarAsync(SqliteConnection connection, string sql, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }
}
