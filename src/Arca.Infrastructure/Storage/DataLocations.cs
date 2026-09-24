// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Infrastructure.Storage;

/// <summary>Where ARCA keeps its files: the data folder, the local settings file and the default database path.</summary>
public sealed record DataLocations(bool IsPortable, string DataFolder, string SettingsFile, string DefaultDatabasePath)
{
    /// <summary>The technical log lives with the data, so in portable mode it stays in the executable's folder too.</summary>
    public string LogFolder => Path.Combine(DataFolder, "logs");

    /// <summary>A file with this name next to the executable turns on portable mode (arquitectura-base, D6).</summary>
    public const string PortableMarker = "arca.portable";

    public const string DatabaseFileName = "arca.db";
    public const string SettingsFileName = "settings.json";
    const string PortableDataFolder = "data";

    /// <summary>
    /// Portable mode keeps everything inside the executable's folder. Otherwise it is the user's own data
    /// folder for the operating system, which never needs administrator rights.
    /// </summary>
    public static DataLocations Resolve(PlatformContext context)
    {
        var portable = File.Exists(Path.Combine(context.ExecutableFolder, PortableMarker));
        var folder = portable
            ? Path.Combine(context.ExecutableFolder, PortableDataFolder)
            : UserDataFolder(context);
        return new DataLocations(
            portable, folder, Path.Combine(folder, SettingsFileName), Path.Combine(folder, DatabaseFileName));
    }

    /// <summary>The path the user configured, or the default one. An explicit choice wins in both modes.</summary>
    public string DatabasePathFor(LocalSettings settings) =>
        string.IsNullOrWhiteSpace(settings.DatabasePath) ? DefaultDatabasePath : settings.DatabasePath;

    /// <summary>Creates the data folder if missing. Only for the folders ARCA owns, never for a path the user chose.</summary>
    public void EnsureDataFolder() => Directory.CreateDirectory(DataFolder);

    static string UserDataFolder(PlatformContext c) => c.Platform switch
    {
        PlatformKind.Windows => Path.Combine(c.LocalAppData ?? Path.Combine(c.HomeFolder, "AppData", "Local"), "ARCA"),
        PlatformKind.MacOS => Path.Combine(c.HomeFolder, "Library", "Application Support", "ARCA"),
        _ => Path.Combine(string.IsNullOrEmpty(c.XdgDataHome) ? Path.Combine(c.HomeFolder, ".local", "share") : c.XdgDataHome, "arca"),
    };
}
