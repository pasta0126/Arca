// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.ComponentModel;
using Arca.UI.Common;
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
                ThemedText.Title(model.Title),
                new TextBlock { Text = model.Note, TextWrapping = TextWrapping.Wrap },
                ThemedText.WarningNote(model.Warning),
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
