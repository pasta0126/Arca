// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Arca.Application.Security;
using Arca.Infrastructure.Security;
using Microsoft.Data.Sqlite;

namespace Arca.DbEditor;

public sealed record TableInfo(string Name, bool IsView, bool HasRowId);

public sealed record ColumnInfo(string Name, string Type, bool NotNull, bool IsPrimaryKey, string? Default);

/// <summary>One page of a table. When the table has a rowid it is the first value of every row and is not in <see cref="Columns"/>.</summary>
public sealed record DataPage(IReadOnlyList<string> Columns, IReadOnlyList<object?[]> Rows, long Total, bool HasRowId);

public sealed record SqlResult(IReadOnlyList<string> Columns, IReadOnlyList<object?[]> Rows, int Affected);

public sealed class OpenException(string message) : Exception(message);

/// <summary>
/// Any SQLite file, whatever its schema: plain, SQLCipher 4 with a raw key, or the database of ARCA with its centre password.
/// Edits run inside one transaction that stays open until <see cref="Commit"/> or <see cref="Rollback"/>, and a copy of the file is
/// made before the first write. Not part of the product (it lives in tools/).
/// </summary>
public sealed partial class DbSession : IDisposable
{
    readonly SqliteConnection _connection;
    SqliteTransaction? _transaction;

    DbSession(string path, SqliteConnection connection)
    {
        Path = path;
        _connection = connection;
    }

    public string Path { get; }

    /// <summary>How the file was opened, to show it.</summary>
    public string Mode { get; private set; } = "";

    public bool HasPendingChanges => _transaction is not null;

    /// <summary>The copy made before the first write of this session, if there was one.</summary>
    public string? BackupPath { get; private set; }

    /// <param name="secret">Empty for a plain file; 64 hexadecimal characters for a raw key; anything else is the centre password of ARCA.</param>
    public static DbSession Open(string path, string? secret)
    {
        path = System.IO.Path.GetFullPath(path);
        if (!File.Exists(path))
        {
            throw new OpenException("No existeix el fitxer.");
        }

        var text = secret?.Trim() ?? "";
        byte[]? key = null;
        string mode;
        if (text.Length == 0)
        {
            mode = "SQLite sense xifrar";
        }
        else if (HexKey().IsMatch(text))
        {
            key = Convert.FromHexString(text);
            mode = "SQLCipher 4 amb clau";
        }
        else
        {
            var unlocked = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore()).Unlock(path, text);
            if (!unlocked.IsSuccess)
            {
                throw new OpenException($"No s'ha pogut desbloquejar amb la contrasenya ({unlocked.Error!.Code}). Cal el fitxer arca.keys al costat de la base.");
            }

            using var dataKey = unlocked.Value!;
            key = dataKey.ToArray();
            mode = "ARCA (contrasenya del centre)";
        }

        var connection = new SqliteConnection($"Data Source={path};Mode=ReadWrite;Pooling=False");
        try
        {
            connection.Open();
            if (key is not null)
            {
                var hex = Convert.ToHexString(key);
                foreach (var pragma in new[] { "PRAGMA cipher='sqlcipher'", "PRAGMA legacy=4", $"PRAGMA key=\"x'{hex}'\"" })
                {
                    Execute(connection, pragma);
                }
            }

            Execute(connection, "PRAGMA foreign_keys=ON");
            Scalar(connection, "SELECT count(*) FROM sqlite_master"); // fails here, not later, if the file does not open
        }
        catch (SqliteException e)
        {
            connection.Dispose();
            throw new OpenException(e.SqliteErrorCode == 26
                ? text.Length == 0
                    ? "El fitxer està xifrat o no és SQLite: escriu la contrasenya del centre o la clau en hexadecimal."
                    : "La clau no obre el fitxer."
                : e.Message);
        }
        finally
        {
            if (key is not null)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
            }
        }

        return new DbSession(path, connection) { Mode = mode };
    }

    public IReadOnlyList<TableInfo> Tables()
    {
        var result = new List<TableInfo>();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT name, type, sql FROM sqlite_master WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%' ORDER BY type, name";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var isView = reader.GetString(1) == "view";
            var withoutRowId = !reader.IsDBNull(2) && reader.GetString(2).Contains("WITHOUT ROWID", StringComparison.OrdinalIgnoreCase);
            result.Add(new TableInfo(reader.GetString(0), isView, !isView && !withoutRowId));
        }

        return result;
    }

    public IReadOnlyList<ColumnInfo> Columns(string table)
    {
        var result = new List<ColumnInfo>();
        using var command = _connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({Quote(table)})";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ColumnInfo(reader.GetString(1), reader.GetString(2), reader.GetInt32(3) != 0, reader.GetInt32(5) != 0,
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return result;
    }

    public DataPage Read(TableInfo table, string filter, int offset, int limit)
    {
        var columns = Columns(table.Name);
        var where = "";
        using var command = _connection.CreateCommand();
        if (filter.Length > 0 && columns.Count > 0)
        {
            where = " WHERE " + string.Join(" OR ", columns.Select(c => $"CAST({Quote(c.Name)} AS TEXT) LIKE $f ESCAPE '\\'"));
            command.Parameters.AddWithValue("$f", "%" + filter.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%");
        }

        command.CommandText = $"SELECT count(*) FROM {Quote(table.Name)}{where}";
        var total = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);

        var select = table.HasRowId ? "rowid, *" : "*";
        command.CommandText = $"SELECT {select} FROM {Quote(table.Name)}{where} ORDER BY {(table.HasRowId ? "rowid" : "1")} LIMIT {limit} OFFSET {offset}";
        using var reader = command.ExecuteReader();
        var skip = table.HasRowId ? 1 : 0;
        var names = Enumerable.Range(skip, reader.FieldCount - skip).Select(reader.GetName).ToList();
        var rows = new List<object?[]>();
        while (reader.Read())
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < row.Length; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return new DataPage(names, rows, total, table.HasRowId);
    }

    /// <summary>Sets a cell. The text «NULL» is NULL and x'AB12' is a blob; anything else goes in as text and the column affinity decides.</summary>
    public void Update(string table, long rowId, string column, string text)
    {
        EnsureTransaction();
        using var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = $"UPDATE {Quote(table)} SET {Quote(column)} = $v WHERE rowid = $r";
        command.Parameters.AddWithValue("$v", ToValue(text));
        command.Parameters.AddWithValue("$r", rowId);
        command.ExecuteNonQuery();
    }

    /// <summary>Adds a row filling only what the table demands (NOT NULL without a default), so the insert is accepted.</summary>
    public long Insert(string table)
    {
        EnsureTransaction();
        var needed = Columns(table).Where(c => c.NotNull && c.Default is null && !(c.IsPrimaryKey && c.Type.Equals("INTEGER", StringComparison.OrdinalIgnoreCase))).ToList();
        using var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = needed.Count == 0
            ? $"INSERT INTO {Quote(table)} DEFAULT VALUES"
            : $"INSERT INTO {Quote(table)} ({string.Join(", ", needed.Select(c => Quote(c.Name)))}) VALUES ({string.Join(", ", needed.Select((_, i) => "$p" + i))})";
        for (var i = 0; i < needed.Count; i++)
        {
            var type = needed[i].Type.ToUpperInvariant();
            command.Parameters.AddWithValue("$p" + i, type.Contains("INT", StringComparison.Ordinal) || type.Contains("REAL", StringComparison.Ordinal)
                || type.Contains("NUM", StringComparison.Ordinal) || type.Contains("BOOL", StringComparison.Ordinal) ? 0L : "");
        }

        command.ExecuteNonQuery();
        return Convert.ToInt64(Scalar(_connection, "SELECT last_insert_rowid()"), CultureInfo.InvariantCulture);
    }

    public int Delete(string table, IEnumerable<long> rowIds)
    {
        EnsureTransaction();
        var count = 0;
        foreach (var id in rowIds)
        {
            using var command = _connection.CreateCommand();
            command.Transaction = _transaction;
            command.CommandText = $"DELETE FROM {Quote(table)} WHERE rowid = $r";
            command.Parameters.AddWithValue("$r", id);
            count += command.ExecuteNonQuery();
        }

        return count;
    }

    /// <summary>Runs free SQL. What it writes is pending like any edit until saved.</summary>
    public SqlResult Run(string sql)
    {
        if (!ReadOnlyStatement().IsMatch(sql))
        {
            EnsureTransaction();
        }

        using var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var rows = new List<object?[]>();
        while (reader.Read() && rows.Count < 5000)
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < row.Length; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return new SqlResult(names, rows, reader.RecordsAffected);
    }

    public void Commit()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection.Dispose();
    }

    public static string Display(object? value) => value switch
    {
        null => "NULL",
        byte[] bytes => "x'" + Convert.ToHexString(bytes) + "'",
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    static object ToValue(string text)
    {
        if (text == "NULL")
        {
            return DBNull.Value;
        }

        var blob = BlobLiteral().Match(text);
        return blob.Success ? Convert.FromHexString(blob.Groups[1].Value) : text;
    }

    void EnsureTransaction()
    {
        if (_transaction is not null)
        {
            return;
        }

        // A plain copy of the file goes before anything is written: it keeps the same encryption, so it opens with the same key.
        // Once per session: the copy is the state of the file when the editing began, whatever is saved afterwards.
        if (BackupPath is null)
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var copy = $"{Path}.{stamp}.bak";
            Execute(_connection, "PRAGMA wal_checkpoint(TRUNCATE)");
            File.Copy(Path, copy, overwrite: false);
            BackupPath = copy;
        }

        _transaction = _connection.BeginTransaction();
    }

    static string Quote(string name) => "\"" + name.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    static object? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    /// <summary>A query: nothing to protect, so it runs without a copy and without opening a transaction.</summary>
    [GeneratedRegex(@"^\s*(SELECT|EXPLAIN)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ReadOnlyStatement();

    [GeneratedRegex("^[0-9a-fA-F]{64}$")]
    private static partial Regex HexKey();

    [GeneratedRegex(@"^x'((?:[0-9a-fA-F]{2})*)'$", RegexOptions.IgnoreCase)]
    private static partial Regex BlobLiteral();
}
