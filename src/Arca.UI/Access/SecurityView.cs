// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Arca.UI.Access;

/// <summary>The security section: the permanent reminder and the two actions. Provisional home until the settings screen of ui-shell exists.</summary>
public sealed class SecurityView : UserControl
{
    public SecurityView(SecurityViewModel model)
    {
        ChangeButton = new Button { Content = model.ChangeLabel };
        RegenerateButton = new Button { Content = model.RegenerateLabel };
        ChangeButton.Click += async (_, _) => await model.ChangePasswordAsync();
        RegenerateButton.Click += async (_, _) => await model.RegenerateKeyAsync();
        model.PropertyChanged += (_, _) => Refresh(model);
        Content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = model.Title, FontSize = 20, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = model.Note, TextWrapping = TextWrapping.Wrap },
                new Border
                {
                    BorderBrush = Brushes.DarkOrange,
                    BorderThickness = new Thickness(2, 0, 0, 0),
                    Padding = new Thickness(10, 4),
                    Child = new TextBlock { Text = model.Warning, TextWrapping = TextWrapping.Wrap },
                },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { ChangeButton, RegenerateButton } },
            },
        };
        Refresh(model);
    }

    public Button ChangeButton { get; }

    public Button RegenerateButton { get; }

    void Refresh(SecurityViewModel model)
    {
        ChangeButton.IsEnabled = model.CanAct;
        RegenerateButton.IsEnabled = model.CanAct;
    }
}
