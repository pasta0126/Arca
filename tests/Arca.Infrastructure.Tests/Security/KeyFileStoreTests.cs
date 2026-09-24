// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text;
using Arca.Application.Security;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Tests.Storage;
using Xunit;

namespace Arca.Infrastructure.Tests.Security;

public sealed class KeyFileStoreTests
{
    const string Spec = "acces-i-xifrat/xifrat-de-la-base";
    const string Password = "riu cadira blau gos";
    const string Recovery = "K7F2P9XQ4MABCDEFGHJKMNPQRS";

    static readonly Argon2Parameters _cost = new(1024, 1, 1);
    static readonly NSecKeyCrypto _crypto = new();

    static KeyFile NewFile() =>
        KeyWrapping.Create(_crypto, _crypto.GenerateDataKey(), Encoding.UTF8.GetBytes(Password), Recovery, _cost);

    [Fact]
    public void The_key_file_sits_next_to_the_database_with_its_own_extension()
    {
        Assert.Equal(Path.Combine("data", "arca.arcakeys"), KeyFileStore.PathFor(Path.Combine("data", "arca.db")));
        Assert.Equal(Path.Combine("data", "arca.arcakeys.prev"), KeyFileStore.PreviousPathFor(Path.Combine("data", "arca.db")));
    }

    [Fact]
    [Trait("spec", Spec + ": Derivación resistente a fuerza bruta (parámetros guardados)")]
    public void A_written_file_reads_back_with_its_parameters_salts_and_wrapped_keys()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var file = NewFile();

        KeyFileStore.Write(database, file);
        var read = KeyFileStore.Read(database);

        Assert.True(read.IsSuccess);
        Assert.Equal(file.FormatVersion, read.Value!.FormatVersion);
        Assert.Equal(file.Password.Parameters, read.Value.Password.Parameters);
        Assert.Equal(file.Password.Salt, read.Value.Password.Salt);
        Assert.Equal(file.Password.Wrapped, read.Value.Password.Wrapped);
        Assert.Equal(file.Recovery.Salt, read.Value.Recovery.Salt);
        Assert.Equal(file.Recovery.Wrapped, read.Value.Recovery.Wrapped);
    }

    [Fact]
    [Trait("spec", Spec + ": Abrir la base de datos (con la contraseña)")]
    public void A_file_that_went_through_the_disk_still_unwraps_with_the_password_and_the_recovery_key()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        using var key = _crypto.GenerateDataKey();
        KeyFileStore.Write(database, KeyWrapping.Create(_crypto, key, Encoding.UTF8.GetBytes(Password), Recovery, _cost));

        var read = KeyFileStore.Read(database).Value!;

        Assert.Equal(key.ToArray(), KeyWrapping.UnwrapWithPassword(_crypto, read, Encoding.UTF8.GetBytes(Password)).Value!.ToArray());
        Assert.Equal(key.ToArray(), KeyWrapping.UnwrapWithRecoveryKey(_crypto, read, Recovery).Value!.ToArray());
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero de claves protegido (contenido del fichero)")]
    public void The_file_holds_only_version_parameters_salts_and_wrapped_keys_and_nothing_personal()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        KeyFileStore.Write(database, NewFile());
        var text = File.ReadAllText(KeyFileStore.PathFor(database));

        using var json = System.Text.Json.JsonDocument.Parse(text);
        var names = json.RootElement.EnumerateObject().Select(p => p.Name).Order().ToList();
        var passwordNames = json.RootElement.GetProperty("password").EnumerateObject().Select(p => p.Name).Order().ToList();
        var recoveryNames = json.RootElement.GetProperty("recovery").EnumerateObject().Select(p => p.Name).Order().ToList();

        Assert.Equal(["formatVersion", "password", "recovery"], names);
        Assert.Equal(["kdf", "memoryKiB", "parallelism", "passes", "salt", "wrapped"], passwordNames);
        Assert.Equal(["kdf", "salt", "wrapped"], recoveryNames);
        Assert.DoesNotContain(Password, text, StringComparison.Ordinal);
        Assert.DoesNotContain("cadira", text, StringComparison.Ordinal);
        Assert.DoesNotContain(Recovery, text, StringComparison.Ordinal);
        Assert.DoesNotContain("K7F2P", text, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Abrir la base de datos (fichero de claves ausente)")]
    public void A_missing_file_is_reported_and_nothing_is_created()
    {
        using var dir = new TempDirectory();

        var result = KeyFileStore.Read(dir.File("arca.db"));

        Assert.Equal("Keys.FileMissing", result.Error!.Code);
        Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }

    [Theory]
    [Trait("spec", Spec + ": Abrir la base de datos (fichero de claves dañado)")]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("{ \"formatVersion\": 1 }")]
    [InlineData("{ \"formatVersion\": 1, \"password\": null, \"recovery\": null }")]
    [InlineData("{ \"password\": {}, \"recovery\": {} }")]
    [InlineData("[1,2,3]")]
    [InlineData("{ \"formatVersion\": \"one\" }")]
    public void A_damaged_file_is_reported_and_left_untouched(string content)
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        File.WriteAllText(KeyFileStore.PathFor(database), content);
        var before = File.ReadAllBytes(KeyFileStore.PathFor(database));

        var result = KeyFileStore.Read(database);

        Assert.Equal("Keys.FileDamaged", result.Error!.Code);
        Assert.Equal(before, File.ReadAllBytes(KeyFileStore.PathFor(database)));
    }

    [Fact]
    [Trait("spec", Spec + ": Abrir la base de datos (fichero de claves dañado)")]
    public void A_file_with_bad_encoding_lengths_or_out_of_range_costs_is_damaged()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        KeyFileStore.Write(database, NewFile());
        var good = File.ReadAllText(KeyFileStore.PathFor(database));

        var cases = new[]
        {
            good.Replace("\"memoryKiB\": 1024", "\"memoryKiB\": 4", StringComparison.Ordinal), // below the minimum
            good.Replace("\"memoryKiB\": 1024", "\"memoryKiB\": 2000000000", StringComparison.Ordinal), // would exhaust memory
            good.Replace("\"passes\": 1", "\"passes\": 0", StringComparison.Ordinal),
            good.Replace("\"kdf\": \"argon2id\"", "\"kdf\": \"scrypt\"", StringComparison.Ordinal),
            good.Replace("\"salt\": \"", "\"salt\": \"AAAA", StringComparison.Ordinal), // wrong length
            good.Replace("\"wrapped\": \"", "\"wrapped\": \"!!!", StringComparison.Ordinal), // not base64
        };

        foreach (var text in cases)
        {
            Assert.NotEqual(good, text);
            File.WriteAllText(KeyFileStore.PathFor(database), text);
            Assert.Equal("Keys.FileDamaged", KeyFileStore.Read(database).Error!.Code);
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Abrir la base de datos (versión desconocida)")]
    public void A_file_from_a_newer_version_is_refused_and_left_untouched()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        KeyFileStore.Write(database, NewFile());
        var newer = File.ReadAllText(KeyFileStore.PathFor(database)).Replace("\"formatVersion\": 1", "\"formatVersion\": 2", StringComparison.Ordinal);
        File.WriteAllText(KeyFileStore.PathFor(database), newer);

        var result = KeyFileStore.Read(database);

        Assert.Equal("Keys.UnknownVersion", result.Error!.Code);
        Assert.Equal(newer, File.ReadAllText(KeyFileStore.PathFor(database)));
    }

    [Fact]
    [Trait("spec", Spec + ": Cambiar la contraseña no recifra la base (atomicidad)")]
    public void Replacing_the_file_keeps_the_previous_version_until_it_is_discarded()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var first = NewFile();
        var second = NewFile();

        KeyFileStore.Write(database, first);
        KeyFileStore.Write(database, second);

        Assert.Equal(second.Password.Wrapped, KeyFileStore.Read(database).Value!.Password.Wrapped);
        using var previous = System.Text.Json.JsonDocument.Parse(File.ReadAllText(KeyFileStore.PreviousPathFor(database)));
        var previousWrapped = previous.RootElement.GetProperty("password").GetProperty("wrapped").GetString();
        Assert.Equal(Convert.ToBase64String(first.Password.Wrapped), previousWrapped);

        KeyFileStore.DiscardPrevious(database);

        Assert.False(File.Exists(KeyFileStore.PreviousPathFor(database)));
        Assert.True(KeyFileStore.Read(database).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambiar la contraseña no recifra la base (atomicidad)")]
    public void An_interrupted_write_leaves_the_previous_file_valid_and_no_temporary_behind()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");
        var original = NewFile();
        KeyFileStore.Write(database, original);

        // A crash after the temporary was written but before it replaced the file: the file is what it was.
        File.WriteAllText(KeyFileStore.PathFor(database) + ".tmp", "half written garbage");

        Assert.Equal(original.Password.Wrapped, KeyFileStore.Read(database).Value!.Password.Wrapped);

        // The next write replaces the stale temporary instead of failing on it.
        var next = NewFile();
        KeyFileStore.Write(database, next);
        Assert.Equal(next.Password.Wrapped, KeyFileStore.Read(database).Value!.Password.Wrapped);
        Assert.False(File.Exists(KeyFileStore.PathFor(database) + ".tmp"));
    }

    [Fact]
    public void Discarding_when_there_is_no_previous_version_does_nothing()
    {
        using var dir = new TempDirectory();

        KeyFileStore.DiscardPrevious(dir.File("arca.db"));

        Assert.Empty(Directory.GetFileSystemEntries(dir.Path));
    }
}
