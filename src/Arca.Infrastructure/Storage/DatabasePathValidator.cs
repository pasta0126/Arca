// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Infrastructure.Storage;

/// <summary>Checks that a database path can be used before anything is created or changed.</summary>
public static class DatabasePathValidator
{
    /// <summary>
    /// Passes when the file's folder exists and can be written to. A failing path is reported without
    /// creating or modifying anything. The write check creates and removes an empty probe file, and only after
    /// the folder is known to exist.
    /// </summary>
    public static Result<string> Validate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return Result<string>.Failure(StorageErrors.PathNotAccessible(path ?? string.Empty));
        }

        var full = Path.GetFullPath(path);
        var folder = Path.GetDirectoryName(full);
        if (Directory.Exists(full) || folder is null || !Directory.Exists(folder))
        {
            return Result<string>.Failure(StorageErrors.PathNotAccessible(path));
        }

        return CanWrite(full, folder)
            ? Result<string>.Success(full)
            : Result<string>.Failure(StorageErrors.PathNotAccessible(path));
    }

    static bool CanWrite(string file, string folder)
    {
        try
        {
            if (File.Exists(file))
            {
                using var existing = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                return true;
            }

            var probe = Path.Combine(folder, ".arca-write-check-" + Guid.NewGuid().ToString("N"));
            using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
            {
            }

            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
