// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Arca.DbEditor;

public sealed class EditorApp : Avalonia.Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Arca.DbEditor/"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"),
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var file = desktop.Args is { Length: > 0 } args ? args[0] : null;
            desktop.MainWindow = new EditorWindow(file);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
