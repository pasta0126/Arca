// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Arca.Application.Storage;
using Arca.Infrastructure.Storage;
using Arca.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arca.Infrastructure.Tests.Storage;

public sealed class EncryptedStorageTests
{
    const string Spec = "arquitectura-base/emmagatzematge-local";
    const string Unicode = "Núria Çaragol l·l Nyerro";

    static byte[] Hash(string path) => SHA256.HashData(File.ReadAllBytes(path));

    static async Task<string> CreateWithProbeAsync(TempDirectory dir, DatabaseKey key)
    {
        var path = dir.File("arca.db");
        var created = await ArcaDatabase.CreateAsync(path, key);
        Assert.True(created.IsSuccess);
        await using var context = created.Value!;
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE Probe (Value TEXT NOT NULL)");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO Probe(Value) VALUES ({0})", Unicode);
        return path;
    }

    [Fact]
    [Trait("spec", Spec + ": Base de datos local en un único fichero")]
    public async Task Create_then_open_round_trips_unicode_data()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("round-trip");
        var path = await CreateWithProbeAsync(dir, key);

        var opened = await ArcaDatabase.OpenAsync(path, key);

        Assert.True(opened.IsSuccess);
        await using var context = opened.Value!;
        var values = await context.Database.SqlQueryRaw<string>("SELECT Value AS Value FROM Probe").ToListAsync();
        Assert.Equal(Unicode, Assert.Single(values));
    }

    [Fact]
    [Trait("spec", Spec + ": Base de datos local en un único fichero")]
    public async Task Create_uses_the_sqlcipher_4_format_and_marks_the_file_as_arca()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("format");
        var path = await CreateWithProbeAsync(dir, key);

        var opened = await ArcaDatabase.OpenAsync(path, key);
        await using var context = opened.Value!;
        var cipher = await context.Database.SqlQueryRaw<string>("PRAGMA cipher").ToListAsync();
        var applicationId = await context.Database.SqlQueryRaw<int>("PRAGMA application_id").ToListAsync();
        var integrity = await context.Database.SqlQueryRaw<string>("PRAGMA integrity_check").ToListAsync();

        Assert.Equal("sqlcipher", Assert.Single(cipher));
        Assert.Equal(ArcaDatabase.ApplicationId, Assert.Single(applicationId));
        Assert.Equal("ok", Assert.Single(integrity));
    }

    [Fact]
    public async Task Create_never_overwrites_an_existing_file()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("no-overwrite");
        var path = await CreateWithProbeAsync(dir, key);
        var before = Hash(path);

        var again = await ArcaDatabase.CreateAsync(path, key);

        Assert.Equal("Storage.FileAlreadyExists", again.Error!.Code);
        Assert.Equal(before, Hash(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Cifrado en reposo con la contraseña del centro")]
    public async Task File_header_is_not_the_plain_sqlite_header()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("header");
        var path = await CreateWithProbeAsync(dir, key);

        var head = Encoding.ASCII.GetString(File.ReadAllBytes(path), 0, 15);

        Assert.NotEqual("SQLite format 3", head);
        Assert.DoesNotContain("Probe", Encoding.UTF8.GetString(File.ReadAllBytes(path)), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Cifrado en reposo con la contraseña del centro")]
    public async Task Standard_sqlite_without_the_key_cannot_read_anything()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("no-key");
        var path = await CreateWithProbeAsync(dir, key);
        var before = Hash(path);

        await using (var plain = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False"))
        {
            await plain.OpenAsync();
            await using var command = plain.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master";
            var error = await Assert.ThrowsAsync<SqliteException>(async () => await command.ExecuteScalarAsync());
            Assert.Equal(26, error.SqliteErrorCode); // SQLITE_NOTADB
        }

        Assert.Equal(before, Hash(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Cifrado en reposo con la contraseña del centro")]
    public async Task The_sqlite3_command_line_tool_cannot_read_the_file()
    {
        var tool = new[] { "/usr/bin/sqlite3", "/usr/local/bin/sqlite3", "/opt/homebrew/bin/sqlite3" }.FirstOrDefault(File.Exists);
        Assert.SkipWhen(tool is null, "No standard sqlite3 tool on this machine");

        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("cli");
        var path = await CreateWithProbeAsync(dir, key);

        var start = new ProcessStartInfo(tool!, $"\"{path}\" \"SELECT name FROM sqlite_master;\"") { RedirectStandardError = true, RedirectStandardOutput = true };
        using var process = Process.Start(start)!;
        var stderr = await process.StandardError.ReadToEndAsync();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.True(
            (stderr + stdout).Contains("not a database", StringComparison.OrdinalIgnoreCase),
            $"size={new FileInfo(path).Length} exit={process.ExitCode} stdout=[{stdout}] stderr=[{stderr}] args=[{start.Arguments}]");
        Assert.DoesNotContain("Probe", stdout, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero corrupto o clave incompatible")]
    public async Task Wrong_key_is_reported_and_the_file_is_untouched()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("right");
        using var other = TestKeys.FromSeed("wrong");
        var path = await CreateWithProbeAsync(dir, key);
        var before = Hash(path);

        var result = await ArcaDatabase.OpenAsync(path, other);

        Assert.Equal("Storage.Unreadable", result.Error!.Code);
        Assert.Equal(before, Hash(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero corrupto o clave incompatible")]
    public async Task A_text_file_is_reported_as_unreadable_and_left_alone()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("text");
        var path = dir.File("not-a-db.db");
        await File.WriteAllTextAsync(path, "this is definitely not a database, but it is long enough to be tested".PadRight(4096, '.'));
        var before = Hash(path);

        var result = await ArcaDatabase.OpenAsync(path, key);

        Assert.Equal("Storage.Unreadable", result.Error!.Code);
        Assert.Equal(before, Hash(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero corrupto o clave incompatible")]
    public async Task A_damaged_file_is_reported_as_unreadable_and_left_alone()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("damaged");
        var path = await CreateWithProbeAsync(dir, key);
        var bytes = await File.ReadAllBytesAsync(path);
        for (var i = 0; i < 64; i++)
        {
            bytes[i] ^= 0xFF; // destroys the salt, so the file can no longer be decrypted
        }

        await File.WriteAllBytesAsync(path, bytes);
        var before = Hash(path);

        var result = await ArcaDatabase.OpenAsync(path, key);

        Assert.Equal("Storage.Unreadable", result.Error!.Code);
        Assert.Equal(before, Hash(path));
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero corrupto o clave incompatible")]
    public async Task A_valid_encrypted_database_from_something_else_is_not_an_arca_database()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("foreign");
        var path = await CreateWithProbeAsync(dir, key);
        await using (var context = (await ArcaDatabase.OpenAsync(path, key)).Value!)
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA application_id = 0");
        }

        SqliteConnection.ClearAllPools();
        var before = Hash(path);

        var result = await ArcaDatabase.OpenAsync(path, key);

        Assert.Equal("Storage.NotArcaDatabase", result.Error!.Code);
        Assert.Equal(before, Hash(path));
    }

    [Fact]
    public async Task An_empty_file_is_not_an_arca_database_and_stays_empty()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("empty");
        var path = dir.File("empty.db");
        await File.WriteAllBytesAsync(path, []);

        var result = await ArcaDatabase.OpenAsync(path, key);

        Assert.Equal("Storage.NotArcaDatabase", result.Error!.Code);
        Assert.Equal(0, new FileInfo(path).Length);
    }

    [Fact]
    public async Task A_missing_file_is_reported_and_not_created()
    {
        using var dir = new TempDirectory();
        using var key = TestKeys.FromSeed("missing");
        var path = dir.File("missing.db");

        var result = await ArcaDatabase.OpenAsync(path, key);

        Assert.Equal("Storage.FileNotFound", result.Error!.Code);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void A_key_must_have_exactly_32_bytes()
    {
        Assert.Throws<ArgumentException>(() => new DatabaseKey(new byte[16]));
        Assert.Throws<ArgumentException>(() => new DatabaseKey(new byte[33]));
    }

    [Fact]
    public void Disposing_a_key_wipes_its_bytes()
    {
        var key = TestKeys.FromSeed("wipe");
        key.Dispose();

        Assert.All(key.ToArray(), b => Assert.Equal(0, b));
    }
}
