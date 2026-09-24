// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Arca.UI.Confirmation;

/// <summary>Modal dialog: Escape and the window close button cancel; only the confirm button confirms.</summary>
public sealed class ConfirmationWindow : Window
{
    public ConfirmationWindow(ConfirmationViewModel model)
    {
        Title = model.Title;
        Width = 460;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        ConfirmButton = new Button { Content = model.ConfirmLabel, IsDefault = !model.CancelHasInitialFocus };
        CancelButton = new Button { Content = model.CancelLabel, IsCancel = true, IsDefault = model.CancelHasInitialFocus };
        ConfirmButton.Click += (_, _) => Close(true);
        CancelButton.Click += (_, _) => Close(false);
        _initialFocus = model.CancelHasInitialFocus ? CancelButton : ConfirmButton;

        var body = new StackPanel { Spacing = 12 };
        body.Children.Add(new TextBlock { Text = model.Title, FontSize = 18, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(new TextBlock { Text = model.Consequence, TextWrapping = TextWrapping.Wrap });
        foreach (var line in model.Details)
        {
            body.Children.Add(new TextBlock { Text = "• " + line });
        }

        body.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { CancelButton, ConfirmButton },
        });
        body.Margin = new Thickness(24);
        Content = body;
    }

    readonly Button _initialFocus;

    public Button ConfirmButton { get; }

    public Button CancelButton { get; }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _initialFocus.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(false);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
