// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Arca.UI.Screens;

/// <summary>Modal dialog with one button per way forward and a cancel one; Escape and the close button cancel.</summary>
public sealed class ChoiceWindow : Window
{
    public ChoiceWindow(ChoiceRequest request)
    {
        Title = request.Title;
        Width = 480;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var body = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        body.Children.Add(ThemedText.Title(request.Title));
        body.Children.Add(new TextBlock { Text = request.Message, TextWrapping = TextWrapping.Wrap });
        foreach (var option in request.Options)
        {
            var button = new Button { Content = option.Label, HorizontalAlignment = HorizontalAlignment.Stretch };
            var id = option.Id;
            button.Click += (_, _) => Close(id);
            OptionButtons.Add(id, button);
            body.Children.Add(button);
        }

        CancelButton = new Button { Content = request.CancelLabel, IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right };
        CancelButton.Click += (_, _) => Close(null);
        body.Children.Add(CancelButton);
        Content = body;
    }

    /// <summary>The button of each option, by its name, so a test can press it.</summary>
    public Dictionary<string, Button> OptionButtons { get; } = [];

    public Button CancelButton { get; }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(null);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
