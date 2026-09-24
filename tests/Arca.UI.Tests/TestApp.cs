// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Tests;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Skia;

[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Arca.UI.Tests;

public sealed class TestApp : Avalonia.Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = Arca.UI.Theme.ArcaTheme.Variant;
        Styles.Add(Arca.UI.Theme.ArcaTheme.CreateFluent());
        Resources.MergedDictionaries.Add(Arca.UI.Theme.ArcaTheme.CreateResources());
    }
}

public static class TestAppBuilder
{
    /// <summary>Real drawing is only switched on to take screenshots (ARCA_SCREENSHOT=folder); tests normally skip it.</summary>
    public static AppBuilder BuildAvaloniaApp()
    {
        var drawing = Environment.GetEnvironmentVariable("ARCA_SCREENSHOT") is not null;
        var builder = AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = !drawing });
        return drawing ? builder.UseSkia() : builder;
    }
}
