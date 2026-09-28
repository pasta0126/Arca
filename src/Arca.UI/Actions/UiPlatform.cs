// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.UI.Actions;

/// <summary>The operating system families whose keyboards differ for the shortcuts: Command on macOS, Control elsewhere.</summary>
public enum UiPlatform
{
    Windows,
    Linux,
    MacOS,
}

public static class UiPlatforms
{
    public static UiPlatform Current { get; } =
        OperatingSystem.IsMacOS() ? UiPlatform.MacOS : OperatingSystem.IsWindows() ? UiPlatform.Windows : UiPlatform.Linux;
}
