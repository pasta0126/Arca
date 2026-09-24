// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Arca.UI.Startup;

public sealed class StartupErrorWindow : Window
{
    public StartupErrorWindow(StartupErrorViewModel model)
    {
        Title = model.WindowTitle;
        Width = 520;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        CloseButton = new Button { Content = model.CloseLabel, HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true };
        CloseButton.Click += (_, _) => Close();

        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = model.Title, FontSize = 18, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = model.Message, TextWrapping = TextWrapping.Wrap },
                CloseButton,
            },
        };
    }

    public Button CloseButton { get; }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        CloseButton.Focus();
    }
}
