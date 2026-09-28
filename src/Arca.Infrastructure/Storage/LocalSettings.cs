// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using System.Text.Json.Nodes;
using Arca.Application.Preferences;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// Settings that belong to this computer, not to the centre: they are outside the database and outside
/// the backups. The path of the database and the arrangement of the interface. No personal data.
/// </summary>
public sealed record LocalSettings(string? DatabasePath = null, UiPreferences? Ui = null);

/// <summary>
/// Reads and writes the local settings file. Reading is tolerant and section by section: a file that cannot be read gives
/// the defaults, and a damaged section (say, the interface preferences) never costs the others (say, the database path).
/// </summary>
public sealed class LocalSettingsStore(string file)
{
    static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public LocalSettings Load()
    {
        JsonObject? root;
        try
        {
            root = File.Exists(file) ? JsonNode.Parse(File.ReadAllText(file)) as JsonObject : null;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            root = null;
        }

        return root is null
            ? new LocalSettings()
            : new LocalSettings(Section<string>(root, nameof(LocalSettings.DatabasePath)), Section<UiPreferences>(root, nameof(LocalSettings.Ui)));
    }

    /// <summary>Writes through a temporary file so a crash never leaves a half-written settings file.</summary>
    public void Save(LocalSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        var temp = file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, _options));
        File.Move(temp, file, overwrite: true);
    }

    /// <summary>
    /// Replaces one section of the file and leaves everything else exactly as it is, including sections this version does not
    /// know. If the file exists but cannot be read, nothing is written: rewriting it from defaults would lose what it holds,
    /// such as the path of the database.
    /// </summary>
    /// <returns>False if the file was left alone because it could not be read.</returns>
    public bool SaveSection<T>(string name, T value)
    {
        JsonObject root;
        try
        {
            root = File.Exists(file) ? JsonNode.Parse(File.ReadAllText(file)) as JsonObject ?? throw new JsonException() : [];
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return false;
        }

        root[name] = JsonSerializer.SerializeToNode(value, _options);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        var temp = file + ".tmp";
        File.WriteAllText(temp, root.ToJsonString(_options));
        File.Move(temp, file, overwrite: true);
        return true;
    }

    static T? Section<T>(JsonObject root, string name) where T : class
    {
        try
        {
            return root[name]?.Deserialize<T>(_options);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }
}

/// <summary>The interface preferences, kept in the local settings file next to the path of the database.</summary>
public sealed class LocalUiPreferencesStore(LocalSettingsStore settings) : IUiPreferencesStore
{
    public UiPreferences Load() => settings.Load().Ui ?? new UiPreferences();

    public void Save(UiPreferences preferences) => settings.SaveSection(nameof(LocalSettings.Ui), preferences);
}
