// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Security;
using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Infrastructure.Security;
using Microsoft.Data.Sqlite;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// Creates the encrypted database and its key file as one operation (acces-i-xifrat, D3): the database is built
/// under a temporary name, the key file is written, and only then does the database take its final name. Whatever
/// happens in between, the result is either both files or neither, and an existing database is never touched.
/// </summary>
public static class DatabaseCreator
{
    const string BuildingSuffix = ".new";

    /// <param name="path">Where the database goes.</param>
    /// <param name="access">The new keys. The recovery key must have been confirmed by typing the groups asked.</param>
    /// <param name="typedGroups">The groups typed to confirm the recovery key.</param>
    public static async Task<Result<bool>> CreateAsync(
        string path, NewAccess access, IReadOnlyList<string?>? typedGroups, CancellationToken ct = default)
    {
        var mistake = access.Challenge.Verify(typedGroups);
        if (mistake is not null)
        {
            return Result<bool>.Failure(mistake);
        }

        if (File.Exists(path))
        {
            return Result<bool>.Failure(StorageErrors.FileAlreadyExists(path));
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }

        var building = path + BuildingSuffix;
        var keyFile = KeyFileStore.PathFor(path);
        DeleteQuietly(building); // leftovers of an earlier attempt that never finished
        var moved = false;
        try
        {
            var created = await ArcaDatabase.CreateAsync(building, access.DataKey, ct);
            if (!created.IsSuccess)
            {
                return Result<bool>.Failure(created.Error!);
            }

            await created.Value!.DisposeAsync();
            SqliteConnection.ClearAllPools();

            KeyFileStore.Write(path, access.KeyFile);
            File.Move(building, path);
            moved = true;
            DiscardPreviousQuietly(path);
            return Result<bool>.Success(true);
        }
        catch
        {
            SqliteConnection.ClearAllPools();
            DeleteQuietly(building);
            if (!moved)
            {
                // The database never took its place, so the key file written for it must not stay next to whatever is
                // there: put back the one it replaced (a leftover of an earlier attempt or another database's), or remove it.
                var previous = KeyFileStore.PreviousPathFor(path);
                try
                {
                    if (File.Exists(previous))
                    {
                        File.Move(previous, keyFile, overwrite: true);
                    }
                    else
                    {
                        DeleteQuietly(keyFile);
                    }
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // Both files are still where they were; nothing is deleted.
                }
            }

            throw;
        }
    }

    static void DiscardPreviousQuietly(string path)
    {
        try
        {
            KeyFileStore.DiscardPrevious(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A leftover previous key file is harmless and the next unlock removes it.
        }
    }

    static void DeleteQuietly(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A leftover file is harmless; the next attempt removes it.
        }
    }
}
