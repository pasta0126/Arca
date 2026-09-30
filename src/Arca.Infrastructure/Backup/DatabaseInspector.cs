// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Backup;
using Arca.Application.Storage;
using Arca.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Backup;

/// <summary>
/// Looks inside a database file without changing it (copies-de-seguretat, D3 and D4): which schema it has compared with the one this
/// version knows, and how many of each thing it holds. It opens the file read-only with its key and never reads a name.
/// </summary>
public static class DatabaseInspector
{
    /// <summary>The schema of the file against the ones this version of the application knows.</summary>
    public static async Task<SchemaRelation> ClassifyAsync(string path, DatabaseKey key, CancellationToken ct = default)
    {
        string[] known;
        await using (var probe = new ArcaDbContext(path, key))
        {
            known = [.. probe.Database.GetMigrations()];
        }

        var applied = await AppliedAsync(path, key, ct);
        return applied.Except(known, StringComparer.Ordinal).Any() ? SchemaRelation.Newer
            : known.Except(applied, StringComparer.Ordinal).Any() ? SchemaRelation.Older
            : SchemaRelation.Same;
    }

    /// <summary>How many years, students, active lockers and assignments the file holds. A table an older schema does not have counts as none.</summary>
    public static async Task<ContentCounts> CountAsync(string path, DatabaseKey key, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync(ct);
        SqlCipherKeyInterceptor.Unlock(connection, key);
        return new ContentCounts(
            await CountAsync(connection, "AcademicYears", null, ct),
            await CountAsync(connection, "Students", null, ct),
            await CountAsync(connection, "Lockers", "\"RetiredAtUtc\" IS NULL", ct),
            await CountAsync(connection, "Assignments", null, ct));
    }

    /// <summary>The migrations a file has applied, read without changing it. The migrator uses the same reading, so a file is judged the same way everywhere.</summary>
    public static async Task<string[]> AppliedAsync(string path, DatabaseKey key, CancellationToken ct)
    {
        await using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync(ct);
        SqlCipherKeyInterceptor.Unlock(connection, key);
        if (!await TableExistsAsync(connection, "__EFMigrationsHistory", ct))
        {
            return [];
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory";
        var ids = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            ids.Add(reader.GetString(0));
        }

        return [.. ids];
    }

    static async Task<int> CountAsync(SqliteConnection connection, string table, string? where, CancellationToken ct)
    {
        if (!await TableExistsAsync(connection, table, ct))
        {
            return 0;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{table}\"" + (where is null ? string.Empty : " WHERE " + where);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    static async Task<bool> TableExistsAsync(SqliteConnection connection, string table, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name";
        command.Parameters.AddWithValue("$name", table);
        return await command.ExecuteScalarAsync(ct) is not null;
    }
}
