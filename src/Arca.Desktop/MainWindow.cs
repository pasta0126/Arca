// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application;
using Arca.UI.Access;
using Arca.UI.Info;
using Arca.UI.Layout;
using Arca.UI.Preferences;
using Avalonia.Controls;

namespace Arca.Desktop;

/// <summary>Placeholder window that proves the shell starts. Replaced by the ui-shell change.</summary>
public sealed class MainWindow : Window
{
    public MainWindow(AppInfo info, ILocalizer localizer, SecurityViewModel security, UiPreferencesSession preferences)
    {
        Title = localizer.Get("App.Label.Title");
        WindowStateKeeper.Attach(this, preferences);
        var securitySection = new CollapsibleSectionViewModel(
            "security", security.Title, () => security.Note, preferences);
        Content = new StackPanel
        {
            Children =
            {
                new InfoView(new InfoViewModel(info, localizer)),
                new CollapsibleSectionView(securitySection, new SecurityView(security)) { Margin = new Avalonia.Thickness(24, 0) },
            },
        };
    }
}
