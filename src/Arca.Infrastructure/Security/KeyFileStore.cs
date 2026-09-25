// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Application.Security;
using Arca.Domain.Common;

namespace Arca.Infrastructure.Security;

/// <summary>
/// Reads and writes the key file that sits next to the database (acces-i-xifrat, D3). It is a small JSON file with the
/// version, parameters, salts and the two wrapped keys, and nothing personal. Writing goes through a temporary file and
/// keeps the previous version until the next successful unlock, so an interruption never leaves the only copy half written.
/// </summary>
public static class KeyFileStore
{
    public const string Extension = ".arcakeys";
    public const string PreviousSuffix = ".prev";

    const int MinMemoryKiB = 8;
    const int MaxMemoryKiB = 1024 * 1024;
    const int MaxPasses = 100;
    const int MaxParallelism = 16;

    static readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>The key file of a database: same name, own extension.</summary>
    public static string PathFor(string databasePath) => Path.ChangeExtension(databasePath, Extension);

    public static string PreviousPathFor(string databasePath) => PathFor(databasePath) + PreviousSuffix;

    /// <summary>
    /// Reads the key file. A missing, damaged or newer-version file is reported without touching anything.
    /// </summary>
    public static Result<KeyFile> Read(string databasePath)
    {
        var path = PathFor(databasePath);
        if (!File.Exists(path))
        {
            return Result<KeyFile>.Failure(KeyErrors.FileMissing(path));
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            var root = document.RootElement;
            if (!root.TryGetProperty("formatVersion", out var versionElement) || !versionElement.TryGetInt32(out var version))
            {
                return Result<KeyFile>.Failure(KeyErrors.FileDamaged(path));
            }

            if (version != KeyFile.CurrentVersion)
            {
                return Result<KeyFile>.Failure(KeyErrors.UnknownVersion(path));
            }

            var dto = root.Deserialize<Dto>(_json);
            var file = dto?.ToKeyFile();
            return file is null ? Result<KeyFile>.Failure(KeyErrors.FileDamaged(path)) : Result<KeyFile>.Success(file);
        }
        catch (Exception e) when (e is JsonException or FormatException or InvalidOperationException or IOException)
        {
            return Result<KeyFile>.Failure(KeyErrors.FileDamaged(path));
        }
    }

    /// <summary>Writes the key file atomically, keeping the version it replaces as the previous one.</summary>
    public static void Write(string databasePath, KeyFile file)
    {
        var path = PathFor(databasePath);
        var temp = path + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, Dto.From(file), _json);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(path))
            {
                File.Copy(path, PreviousPathFor(databasePath), overwrite: true);
            }

            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }

    /// <summary>Removes the previous version, once a later unlock has proved the current one works.</summary>
    public static void DiscardPrevious(string databasePath)
    {
        var previous = PreviousPathFor(databasePath);
        if (File.Exists(previous))
        {
            File.Delete(previous);
        }
    }

    sealed record Dto(int FormatVersion, PasswordDto? Password, RecoveryDto? Recovery)
    {
        public static Dto From(KeyFile f) => new(
            f.FormatVersion,
            new PasswordDto(
                f.Password.Kdf, f.Password.Parameters.MemoryKiB, f.Password.Parameters.Passes, f.Password.Parameters.Parallelism,
                Convert.ToBase64String(f.Password.Salt), Convert.ToBase64String(f.Password.Wrapped)),
            new RecoveryDto(f.Recovery.Kdf, Convert.ToBase64String(f.Recovery.Salt), Convert.ToBase64String(f.Recovery.Wrapped)));

        /// <summary>Null when anything is missing or out of range: a file like that is treated as damaged.</summary>
        public KeyFile? ToKeyFile()
        {
            if (Password is null || Recovery is null
                || Password.Kdf != KeyWrapping.PasswordKdf || Recovery.Kdf != KeyWrapping.RecoveryKdf
                || Password.MemoryKiB is < MinMemoryKiB or > MaxMemoryKiB
                || Password.Passes is < 1 or > MaxPasses
                || Password.Parallelism is < 1 or > MaxParallelism)
            {
                return null;
            }

            var passwordSalt = Convert.FromBase64String(Password.Salt);
            var recoverySalt = Convert.FromBase64String(Recovery.Salt);
            var passwordWrapped = Convert.FromBase64String(Password.Wrapped);
            var recoveryWrapped = Convert.FromBase64String(Recovery.Wrapped);
            if (passwordSalt.Length != KeyWrapping.SaltLength || recoverySalt.Length != KeyWrapping.SaltLength
                || passwordWrapped.Length != WrappedLength || recoveryWrapped.Length != WrappedLength)
            {
                return null;
            }

            return new KeyFile(
                FormatVersion,
                new PasswordSlot(Password.Kdf, new Argon2Parameters(Password.MemoryKiB, Password.Passes, Password.Parallelism), passwordSalt, passwordWrapped),
                new RecoverySlot(Recovery.Kdf, recoverySalt, recoveryWrapped));
        }

        /// <summary>Nonce (24) + key (32) + tag (16).</summary>
        const int WrappedLength = 24 + 32 + 16;
    }

    sealed record PasswordDto(string Kdf, int MemoryKiB, int Passes, int Parallelism, string Salt, string Wrapped);

    sealed record RecoveryDto(string Kdf, string Salt, string Wrapped);
}
