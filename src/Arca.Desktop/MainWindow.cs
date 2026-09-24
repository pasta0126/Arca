// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia.Controls;

namespace Arca.Desktop;

/// <summary>Placeholder window that proves the shell starts. Replaced by the ui-shell change.</summary>
public sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "ARCA";
        Width = 1024;
        Height = 640;
        MinWidth = 1024;
        MinHeight = 640;
    }
}
