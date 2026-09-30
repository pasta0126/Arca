// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Globalization;
using Arca.Application.Backup;
using Arca.Domain.Common;

namespace Arca.Infrastructure.Backup;

/// <summary>
/// Where a backup goes (copia-de-seguretat, Nombre y destino de la copia y Destino no válido): the name proposed, and the checks that are
/// made before anything is written, so a wrong destination is refused with its reason and nothing is generated for nothing.
/// </summary>
public static class BackupDestination
{
    /// <summary>The name proposed for a backup made now, with the date and the time and the extension of ARCA: ARCA-copia-2026-09-24-1030.arcabackup.</summary>
    public static string SuggestedName(DateTimeOffset moment) =>
        "ARCA-copia-" + moment.ToString("yyyy-MM-dd-HHmm", CultureInfo.InvariantCulture) + BackupContainer.Extension;

    /// <summary>
    /// Checks a destination: it is not the database itself, its folder exists and accepts writing and has room for a copy of the database.
    /// </summary>
    /// <param name="destination">The file the backup will be written to.</param>
    /// <param name="databasePath">The database, which can never be the destination.</param>
    /// <returns>The reason it is refused, or null when it is fine.</returns>
    public static Error? Check(string destination, string databasePath)
    {
        var target = Path.GetFullPath(destination);
        var comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var sensitive = new[] { Path.GetFullPath(databasePath), Path.GetFullPath(Arca.Infrastructure.Security.KeyFileStore.PathFor(databasePath)) };
        if (sensitive.Any(p => string.Equals(p, target, comparison)))
        {
            return BackupErrors.DestinationIsDatabase;
        }

        var folder = Path.GetDirectoryName(target)!;
        if (!Directory.Exists(folder))
        {
            return BackupErrors.FolderMissing(folder);
        }

        if (!CanWriteIn(folder))
        {
            return BackupErrors.FolderNotWritable(folder);
        }

        var needed = File.Exists(databasePath) ? new FileInfo(databasePath).Length : 0;
        try
        {
            var free = new DriveInfo(Path.GetPathRoot(target)!).AvailableFreeSpace;
            if (free < needed * 2 + 1_000_000) // the snapshot and the container are made side by side before the snapshot goes
            {
                return BackupErrors.NotEnoughSpace(Math.Max(1, needed * 2 / 1_000_000));
            }
        }
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException)
        {
            // The free space cannot be known here (a network share): carry on, the write itself will say if it does not fit.
        }

        return null;
    }

    static bool CanWriteIn(string folder)
    {
        var probe = Path.Combine(folder, ".arca-write-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllBytes(probe, [0]);
            File.Delete(probe);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
