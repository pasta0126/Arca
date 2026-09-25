// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Security.Cryptography;
using Arca.Application.Security;
using Arca.Application.Startup;
using Arca.Domain.Common;
using Arca.Application.Storage;
using Arca.Infrastructure.Security;
using Arca.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arca.Infrastructure.Tests.Storage;

/// <summary>The encrypted database with the keys of acces-i-xifrat: create, open, migrate and fail safely.</summary>
public sealed class EncryptedAccessTests
{
    const string Spec = "acces-i-xifrat/xifrat-de-la-base";
    const string Password = "riu cadira blau gos";

    static readonly Argon2Parameters _cost = new(1024, 1, 1);

    static AccessService Service() => new(new NSecKeyCrypto(), new FileKeyFileStore(), _cost);

    static byte[] Hash(string path) => SHA256.HashData(File.ReadAllBytes(path));

    static string[] Answers(NewAccess access) => [.. access.Challenge.Indices.Select(i => RecoveryKey.Groups(access.RecoveryKey)[i])];

    static async Task<(string Path, NewAccess Access)> CreateAsync(TempDirectory dir)
    {
        var path = dir.File("arca.db");
        var access = Service().CreateAccess(Password, Password).Value!;
        var created = await DatabaseCreator.CreateAsync(path, access, Answers(access));
        Assert.True(created.IsSuccess);
        return (path, access);
    }

    sealed class CountingFlow : IUnlockFlow
    {
        public int Asked { get; private set; }

        public Task<Result<DatabaseKey>> UnlockAsync(string databasePath, CancellationToken ct)
        {
            Asked++;
            return Task.FromResult(Result<DatabaseKey>.Failure(KeyErrors.UnlockCancelled));
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Cifrado con llave aleatoria (Llave única por base)")]
    public async Task Creating_makes_both_files_and_a_different_key_for_every_database()
    {
        using var first = new TempDirectory();
        using var second = new TempDirectory();

        var (path, access) = await CreateAsync(first);
        var (_, other) = await CreateAsync(second);

        Assert.True(File.Exists(path));
        Assert.True(File.Exists(KeyFileStore.PathFor(path)));
        Assert.False(File.Exists(path + ".new"));
        Assert.NotEqual(access.DataKey.ToArray(), other.DataKey.ToArray());
    }

    [Fact]
    [Trait("spec", Spec + ": Cifrado con llave aleatoria (Base ilegible sin la llave)")]
    public async Task The_created_database_opens_only_with_the_key_the_password_unwraps()
    {
        using var dir = new TempDirectory();
        var (path, access) = await CreateAsync(dir);

        var unlocked = Service().Unlock(path, Password).Value!;
        var opened = await ArcaDatabase.OpenAsync(path, unlocked);
        var wrong = await ArcaDatabase.OpenAsync(path, Arca.Testing.TestKeys.FromSeed("not the key"));

        Assert.True(opened.IsSuccess);
        await opened.Value!.DisposeAsync();
        Assert.Equal("Storage.Unreadable", wrong.Error!.Code);
        Assert.Equal(access.DataKey.ToArray(), unlocked.ToArray());
    }

    [Fact]
    [Trait("spec", Spec + ": Abrir la base de datos (Con la contraseña)")]
    public async Task Another_build_with_the_right_password_opens_the_same_files()
    {
        using var dir = new TempDirectory();
        var (path, _) = await CreateAsync(dir);
        var moved = new TempDirectory();
        File.Copy(path, moved.File("arca.db"));
        File.Copy(KeyFileStore.PathFor(path), KeyFileStore.PathFor(moved.File("arca.db")));

        using var key = new AccessService(new NSecKeyCrypto(), new FileKeyFileStore()).Unlock(moved.File("arca.db"), Password).Value!;
        var opened = await ArcaDatabase.OpenAsync(moved.File("arca.db"), key);

        Assert.True(opened.IsSuccess);
        await opened.Value!.DisposeAsync();
        moved.Dispose();
    }

    [Fact]
    [Trait("spec", Spec + ": Cifrado con llave aleatoria (Llave única por base)")]
    public async Task Creating_without_confirming_the_recovery_key_creates_nothing()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        using var access = Service().CreateAccess(Password, Password).Value!;

        var none = await DatabaseCreator.CreateAsync(path, access, null);
        var wrong = await DatabaseCreator.CreateAsync(path, access, ["XXXXX", "YYYYY"]);

        Assert.Equal("Keys.ConfirmationRequired", none.Error!.Code);
        Assert.Equal("Keys.ConfirmationIncorrect", wrong.Error!.Code);
        Assert.Empty(Directory.EnumerateFiles(dir.Path));
    }

    [Fact]
    [Trait("spec", Spec + ": Cifrado con llave aleatoria (Llave única por base)")]
    public async Task Creating_never_overwrites_an_existing_database_or_its_key_file()
    {
        using var dir = new TempDirectory();
        var (path, _) = await CreateAsync(dir);
        var database = Hash(path);
        var keys = Hash(KeyFileStore.PathFor(path));
        using var another = Service().CreateAccess(Password, Password).Value!;

        var result = await DatabaseCreator.CreateAsync(path, another, Answers(another));

        Assert.Equal("Storage.FileAlreadyExists", result.Error!.Code);
        Assert.Equal(database, Hash(path));
        Assert.Equal(keys, Hash(KeyFileStore.PathFor(path)));
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero de claves protegido (Contenido del fichero)")]
    public async Task An_interrupted_creation_leaves_neither_file_and_can_be_repeated()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        Directory.CreateDirectory(path + ".new"); // makes the temporary name unusable, so the creation fails midway
        using var access = Service().CreateAccess(Password, Password).Value!;

        await Assert.ThrowsAnyAsync<Exception>(() => DatabaseCreator.CreateAsync(path, access, Answers(access)));

        Assert.False(File.Exists(path));
        Assert.False(File.Exists(KeyFileStore.PathFor(path)));
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero de claves protegido (Contenido del fichero)")]
    public async Task Leftovers_of_an_earlier_attempt_do_not_stop_a_new_creation()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        File.WriteAllText(path + ".new", "half built");
        File.WriteAllText(KeyFileStore.PathFor(path), "stale");
        using var access = Service().CreateAccess(Password, Password).Value!;

        var result = await DatabaseCreator.CreateAsync(path, access, Answers(access));

        Assert.True(result.IsSuccess);
        Assert.True(Service().Unlock(path, Password).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Fichero de claves protegido (Contenido del fichero)")]
    public async Task A_failed_creation_puts_back_the_key_file_it_had_replaced()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        File.WriteAllText(KeyFileStore.PathFor(path), "someone else's key file");
        Directory.CreateDirectory(path); // the database cannot take its place, after the key file was written
        using var access = Service().CreateAccess(Password, Password).Value!;

        await Assert.ThrowsAnyAsync<Exception>(() => DatabaseCreator.CreateAsync(path, access, Answers(access)));

        Assert.Equal("someone else's key file", File.ReadAllText(KeyFileStore.PathFor(path)));
        Assert.False(File.Exists(KeyFileStore.PreviousPathFor(path)));
        Assert.False(File.Exists(path + ".new"));
    }

    [Theory]
    [Trait("spec", Spec + ": Abrir la base de datos (Fichero de claves ausente)")]
    [InlineData("missing", "Keys.FileMissing")]
    [InlineData("damaged", "Keys.FileDamaged")]
    [InlineData("altered", "Keys.FileDamaged")]
    public async Task A_missing_or_damaged_key_file_is_reported_before_asking_and_changes_nothing(string state, string code)
    {
        using var dir = new TempDirectory();
        var (path, _) = await CreateAsync(dir);
        var keyFile = KeyFileStore.PathFor(path);
        switch (state)
        {
            case "missing":
                File.Delete(keyFile);
                break;
            case "damaged":
                File.WriteAllText(keyFile, "{ not json");
                break;
            default:
                File.WriteAllText(keyFile, File.ReadAllText(keyFile).Replace("\"passes\": 1", "\"passes\": 0"));
                break;
        }

        var database = Hash(path);
        var keyBefore = File.Exists(keyFile) ? Hash(keyFile) : null;
        var prompt = new CountingFlow();

        var result = await new PasswordKeyProvider(Service(), prompt).GetKeyAsync(path);

        Assert.Equal(code, result.Error!.Code);
        Assert.Equal(0, prompt.Asked); // it does not ask for a password that cannot help
        Assert.Equal(database, Hash(path));
        Assert.Equal(keyBefore, File.Exists(keyFile) ? Hash(keyFile) : null);
    }

    [Fact]
    [Trait("spec", Spec + ": Abrir la base de datos (Fichero de claves ausente)")]
    public async Task The_message_for_a_missing_key_file_offers_restoring_a_copy()
    {
        using var dir = new TempDirectory();
        var (path, _) = await CreateAsync(dir);
        File.Delete(KeyFileStore.PathFor(path));
        var result = await new PasswordKeyProvider(Service(), new CountingFlow()).GetKeyAsync(path);

        var text = new Arca.Application.Localization.ResxLocalizer().Get(Arca.Application.Localization.ResourceKeys.For(result.Error!));

        Assert.Contains("Restaura una còpia", text, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Migraciones y copias previas con la misma llave (Migración)")]
    public async Task A_migrated_database_opens_with_the_same_password_and_its_copy_carries_the_key_file()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        using var access = Service().CreateAccess(Password, Password).Value!;
        await new SchemaMigrator(() => new V1Context(path, access.DataKey, true)).ApplyToNewDatabaseAsync();
        SqliteConnection.ClearAllPools();
        KeyFileStore.Write(path, access.KeyFile);

        using var key = Service().Unlock(path, Password).Value!;
        var result = await new SchemaMigrator(() => new V2Context(path, key)).MigrateAsync(path, key);

        Assert.True(result.IsSuccess);
        var copy = Assert.Single(SchemaMigrator.CopiesOf(path));
        Assert.True(File.Exists(SchemaMigrator.KeyFileCopyOf(copy)));
        Assert.Equal(Hash(KeyFileStore.PathFor(path)), Hash(SchemaMigrator.KeyFileCopyOf(copy)));
        using var again = Service().Unlock(path, Password).Value!;
        await using var context = new ArcaDbContext(path, again);
        var history = await context.Database.SqlQueryRaw<string>("SELECT MigrationId AS Value FROM __EFMigrationsHistory ORDER BY MigrationId").ToListAsync();
        Assert.Equal(["001_CreateA", "002_CreateB"], history);
    }

    [Fact]
    [Trait("spec", Spec + ": Migraciones y copias previas con la misma llave (Migración)")]
    public async Task Old_copies_are_removed_together_with_their_key_file()
    {
        using var dir = new TempDirectory();
        var path = dir.File("arca.db");
        using var access = Service().CreateAccess(Password, Password).Value!;
        await new SchemaMigrator(() => new V1Context(path, access.DataKey, true)).ApplyToNewDatabaseAsync();
        SqliteConnection.ClearAllPools();
        KeyFileStore.Write(path, access.KeyFile);
        for (var day = 10; day < 15; day++)
        {
            var old = $"{path}.premigration-202001{day}T000000000Z.bak";
            File.WriteAllText(old, "old");
            File.WriteAllText(SchemaMigrator.KeyFileCopyOf(old), "old keys");
        }

        var result = await new SchemaMigrator(() => new V2Context(path, access.DataKey)).MigrateAsync(path, access.DataKey);

        Assert.True(result.IsSuccess);
        var kept = SchemaMigrator.CopiesOf(path);
        Assert.Equal(SchemaMigrator.CopiesToKeep, kept.Count);
        Assert.Equal(kept.Count, Directory.EnumerateFiles(dir.Path, "*.bak.arcakeys").Count());
    }
}
