// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// Settings that belong to this computer, not to the centre: they are outside the database and outside
/// the backups. More arrive with the interface (window size, collapsed sections). No personal data.
/// </summary>
public sealed record LocalSettings(string? DatabasePath = null);

/// <summary>Reads and writes the local settings file. Reading is tolerant: anything wrong gives the defaults.</summary>
public sealed class LocalSettingsStore(string file)
{
    static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public LocalSettings Load()
    {
        try
        {
            if (!File.Exists(file))
            {
                return new LocalSettings();
            }

            return JsonSerializer.Deserialize<LocalSettings>(File.ReadAllText(file), _options) ?? new LocalSettings();
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return new LocalSettings();
        }
    }

    /// <summary>Writes through a temporary file so a crash never leaves a half-written settings file.</summary>
    public void Save(LocalSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        var temp = file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, _options));
        File.Move(temp, file, overwrite: true);
    }
}
