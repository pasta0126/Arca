// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text;
using Arca.Application.Security;
using Arca.Domain.Common;
using Arca.Infrastructure.Security;

namespace Arca.Infrastructure.Backup;

/// <summary>
/// The backup file (acces-i-xifrat, D7): a plain container, no compression, holding the encrypted database and the key
/// file that opens it. Layout: the marker "ARCABK01", the number of entries, and for each one its name, its length
/// and its bytes. It is written under a temporary name and only then given its own, so an interrupted write never
/// leaves something that looks like a backup.
/// </summary>
public static class BackupContainer
{
    public const string Extension = ".arcabackup";

    const string Marker = "ARCABK01";
    const string DatabaseEntry = "database";
    const string KeysEntry = "keys";
    const int MaxNameLength = 64;

    /// <summary>What was taken out of a container, in a folder the caller owns.</summary>
    /// <param name="DatabasePath">The extracted database.</param>
    /// <param name="KeyFilePath">The extracted key file, named after the database so the usual key file lookup finds it.</param>
    public sealed record Extracted(string DatabasePath, string KeyFilePath);

    /// <summary>Writes the container from a database file and the key file next to it.</summary>
    public static void Write(string containerPath, string databasePath, string keyFilePath)
    {
        var temp = containerPath + ".tmp";
        try
        {
            using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(Encoding.ASCII.GetBytes(Marker));
                writer.Write(2);
                WriteEntry(writer, output, DatabaseEntry, databasePath);
                WriteEntry(writer, output, KeysEntry, keyFilePath);
                writer.Flush();
                output.Flush(flushToDisk: true);
            }

            File.Move(temp, containerPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }

    /// <summary>Takes the two files out into a folder. A file that is not a complete ARCA backup is reported as damaged.</summary>
    public static Result<Extracted> Extract(string containerPath, string folder)
    {
        var database = Path.Combine(folder, "arca.db");
        var keys = KeyFileStore.PathFor(database);
        try
        {
            using var input = new FileStream(containerPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(input, Encoding.UTF8, leaveOpen: true);
            if (Encoding.ASCII.GetString(reader.ReadBytes(Marker.Length)) != Marker || reader.ReadInt32() != 2)
            {
                return Damaged(containerPath);
            }

            Directory.CreateDirectory(folder);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < 2; i++)
            {
                var nameLength = reader.ReadInt32();
                if (nameLength is < 1 or > MaxNameLength)
                {
                    return Damaged(containerPath);
                }

                var name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
                var length = reader.ReadInt64();
                var target = name switch { DatabaseEntry => database, KeysEntry => keys, _ => null };
                if (target is null || !seen.Add(name) || length < 0 || length > input.Length - input.Position)
                {
                    return Damaged(containerPath);
                }

                using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None);
                CopyExactly(input, output, length);
            }

            return input.Position == input.Length
                ? Result<Extracted>.Success(new Extracted(database, keys))
                : Damaged(containerPath);
        }
        catch (Exception e) when (e is IOException or EndOfStreamException or ArgumentException or DecoderFallbackException)
        {
            return Damaged(containerPath);
        }
    }

    static Result<Extracted> Damaged(string path) => Result<Extracted>.Failure(KeyErrors.BackupDamaged(path));

    static void WriteEntry(BinaryWriter writer, FileStream output, string name, string path)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        writer.Write(nameBytes.Length);
        writer.Write(nameBytes);
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        writer.Write(input.Length);
        writer.Flush();
        input.CopyTo(output);
    }

    static void CopyExactly(Stream input, Stream output, long length)
    {
        var buffer = new byte[81920];
        while (length > 0)
        {
            var read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, length));
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            output.Write(buffer, 0, read);
            length -= read;
        }
    }
}
