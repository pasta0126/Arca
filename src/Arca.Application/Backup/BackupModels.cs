// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Backup;

/// <summary>How the schema of a file relates to the one this version of the application knows (copies-de-seguretat, D3).</summary>
public enum SchemaRelation
{
    /// <summary>The same schema: it opens as it is.</summary>
    Same,

    /// <summary>An older schema: it is migrated to the current one when restored.</summary>
    Older,

    /// <summary>A schema of a newer version of the application: it cannot be restored here and the application has to be updated.</summary>
    Newer,
}

/// <summary>What a database holds, counted and nothing else (copies-de-seguretat, D4): never a name, never a personal datum.</summary>
/// <param name="Years">How many school years.</param>
/// <param name="Students">How many students, those who left included.</param>
/// <param name="Lockers">How many active lockers.</param>
/// <param name="Assignments">How many assignments, current and past.</param>
public sealed record ContentCounts(int Years, int Students, int Lockers, int Assignments);

/// <summary>A backup that was made: where it is and how big, in bytes.</summary>
public sealed record BackupResult(string Path, long SizeBytes);

/// <summary>A restoration that was done: where the copy of the data that was replaced is, and whether the backup had to be brought up to date.</summary>
public sealed record RestoreOutcome(string PreviousCopyPath, bool Migrated);

/// <summary>What is refused about a backup file or its destination, with the reason in words (copia-de-seguretat, Destino no válido).</summary>
public static class BackupErrors
{
    /// <summary>The destination is the database itself. Copying over it would destroy the data.</summary>
    public static readonly Error DestinationIsDatabase = new("Backup.DestinationIsDatabase");

    /// <summary>The folder of the destination does not exist. Args: {0} the folder.</summary>
    public static Error FolderMissing(string folder) => new("Backup.FolderMissing", Args: [folder]);

    /// <summary>The folder of the destination does not accept writing. Args: {0} the folder.</summary>
    public static Error FolderNotWritable(string folder) => new("Backup.FolderNotWritable", Args: [folder]);

    /// <summary>There is not room for the copy. Args: {0} the space needed, in MB.</summary>
    public static Error NotEnoughSpace(long megabytes) => new("Backup.NotEnoughSpace", Args: [megabytes]);

    /// <summary>The copy could not be made or did not pass its check, and nothing was left at the destination.</summary>
    public static readonly Error CopyFailed = new("Backup.CopyFailed");

    /// <summary>The copy of the current data that is made before replacing them could not be made or checked, so nothing was replaced.</summary>
    public static readonly Error PreviousCopyFailed = new("Backup.PreviousCopyFailed");

    /// <summary>The backup is of a version of the application that is newer than this one, which has to be updated first.</summary>
    public static readonly Error VersionNewer = new("Backup.VersionNewer");

    /// <summary>The backup is of an older version and could not be brought up to date, so nothing was replaced.</summary>
    public static readonly Error MigrationFailed = new("Backup.MigrationFailed");
}
