// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Avalonia;

namespace Arca.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        CultureSetup.Apply();
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();
}
