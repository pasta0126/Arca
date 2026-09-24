// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Infrastructure.Storage;

public enum PlatformKind
{
    Windows,
    MacOS,
    Linux,
}

/// <summary>The facts about the machine that decide where data lives. Tests build their own instead of using the real one.</summary>
/// <param name="Platform">Operating system family.</param>
/// <param name="HomeFolder">The user's home folder.</param>
/// <param name="LocalAppData">Windows local application data (null elsewhere).</param>
/// <param name="XdgDataHome">Linux XDG_DATA_HOME when set.</param>
/// <param name="ExecutableFolder">Folder of the running program, where the portable marker is looked for.</param>
public sealed record PlatformContext(
    PlatformKind Platform, string HomeFolder, string? LocalAppData, string? XdgDataHome, string ExecutableFolder)
{
    public static PlatformContext Current()
    {
        var platform = OperatingSystem.IsWindows() ? PlatformKind.Windows
            : OperatingSystem.IsMacOS() ? PlatformKind.MacOS
            : PlatformKind.Linux;
        return new PlatformContext(
            platform,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetEnvironmentVariable("XDG_DATA_HOME"),
            AppContext.BaseDirectory);
    }
}
