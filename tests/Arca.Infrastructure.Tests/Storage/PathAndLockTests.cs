// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Infrastructure.Storage;
using Xunit;

namespace Arca.Infrastructure.Tests.Storage;

public sealed class PathAndLockTests
{
    const string Spec = "arquitectura-base/emmagatzematge-local";

    static string[] Listing(string folder) =>
        [.. Directory.GetFileSystemEntries(folder, "*", SearchOption.AllDirectories).Order()];

    [Fact]
    public void A_path_in_an_existing_writable_folder_is_valid_and_leaves_no_trace()
    {
        using var dir = new TempDirectory();
        var before = Listing(dir.Path);

        var result = DatabasePathValidator.Validate(dir.File("arca.db"));

        Assert.True(result.IsSuccess);
        Assert.Equal(before, Listing(dir.Path)); // the write check cleans up after itself
    }

    [Fact]
    public void An_existing_database_file_in_place_is_valid_and_unchanged()
    {
        using var dir = new TempDirectory();
        var file = dir.File("arca.db");
        File.WriteAllText(file, "data");
        var modified = File.GetLastWriteTimeUtc(file);

        var result = DatabasePathValidator.Validate(file);

        Assert.True(result.IsSuccess);
        Assert.Equal("data", File.ReadAllText(file));
        Assert.Equal(modified, File.GetLastWriteTimeUtc(file));
    }

    [Fact]
    [Trait("spec", Spec + ": Ubicación de la base de datos configurable (ruta no accesible)")]
    public void A_missing_folder_is_rejected_and_not_created()
    {
        using var dir = new TempDirectory();
        var missing = Path.Combine(dir.Path, "does-not-exist", "arca.db");

        var result = DatabasePathValidator.Validate(missing);

        Assert.Equal("Storage.PathNotAccessible", result.Error!.Code);
        Assert.False(Directory.Exists(Path.Combine(dir.Path, "does-not-exist")));
        Assert.Empty(Listing(dir.Path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("spec", Spec + ": Ubicación de la base de datos configurable (ruta no accesible)")]
    public void An_empty_path_is_rejected(string? path)
    {
        Assert.Equal("Storage.PathNotAccessible", DatabasePathValidator.Validate(path).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Ubicación de la base de datos configurable (ruta no accesible)")]
    public void A_folder_given_as_the_file_is_rejected()
    {
        using var dir = new TempDirectory();

        Assert.Equal("Storage.PathNotAccessible", DatabasePathValidator.Validate(dir.Path).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Ubicación de la base de datos configurable (ruta no accesible)")]
    public void A_read_only_folder_is_rejected_and_nothing_is_written()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Folder permissions are checked with Unix modes");
            return;
        }

        using var dir = new TempDirectory();
        var folder = Path.Combine(dir.Path, "locked");
        Directory.CreateDirectory(folder);
        File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            Assert.SkipWhen(Environment.UserName == "root", "root ignores folder permissions");

            var result = DatabasePathValidator.Validate(Path.Combine(folder, "arca.db"));

            Assert.Equal("Storage.PathNotAccessible", result.Error!.Code);
            Assert.Empty(Directory.GetFileSystemEntries(folder));
        }
        finally
        {
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Instancia única sobre una base de datos")]
    public void A_second_instance_on_the_same_database_is_refused()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");

        using var first = InstanceLock.TryAcquire(database).Value;
        var second = InstanceLock.TryAcquire(database);

        Assert.NotNull(first);
        Assert.Equal("Storage.AlreadyRunning", second.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Instancia única sobre una base de datos")]
    public void The_lock_is_released_when_the_first_instance_ends()
    {
        using var dir = new TempDirectory();
        var database = dir.File("arca.db");

        InstanceLock.TryAcquire(database).Value!.Dispose();

        using var again = InstanceLock.TryAcquire(database).Value;
        Assert.NotNull(again);
    }

    [Fact]
    [Trait("spec", Spec + ": Instancia única sobre una base de datos")]
    public void The_lock_belongs_to_the_file_not_to_how_the_path_is_written()
    {
        // An installed copy and a portable copy may reach the same file through different spellings.
        using var dir = new TempDirectory();
        var direct = dir.File("arca.db");
        var indirect = Path.Combine(dir.Path, "sub", "..", "arca.db");
        Directory.CreateDirectory(Path.Combine(dir.Path, "sub"));

        using var first = InstanceLock.TryAcquire(direct).Value;
        var second = InstanceLock.TryAcquire(indirect);

        Assert.Equal("Storage.AlreadyRunning", second.Error!.Code);
    }

    [Fact]
    public void Different_databases_do_not_block_each_other()
    {
        using var dir = new TempDirectory();

        using var one = InstanceLock.TryAcquire(dir.File("one.db")).Value;
        using var two = InstanceLock.TryAcquire(dir.File("two.db")).Value;

        Assert.NotNull(one);
        Assert.NotNull(two);
    }

    [Fact]
    public void A_lock_that_cannot_be_created_counts_as_already_in_use()
    {
        using var dir = new TempDirectory();

        var result = InstanceLock.TryAcquire(Path.Combine(dir.Path, "missing-folder", "arca.db"));

        Assert.Equal("Storage.AlreadyRunning", result.Error!.Code);
    }
}
