// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Tests;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Arca.UI.Tests;

public sealed class TestApp : Avalonia.Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
}

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
