// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application;
using Arca.UI.Info;
using Avalonia.Controls;

namespace Arca.Desktop;

/// <summary>Placeholder window that proves the shell starts. Replaced by the ui-shell change.</summary>
public sealed class MainWindow : Window
{
    public MainWindow(AppInfo info, ILocalizer localizer)
    {
        Title = localizer.Get("App.Label.Title");
        Width = 1024;
        Height = 640;
        MinWidth = 1024;
        MinHeight = 640;
        Content = new InfoView(new InfoViewModel(info, localizer));
    }
}
