// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Arca.UI.Startup;

/// <summary>
/// A light window that appears at once, before the costly stages start, and turns into an error message if
/// starting fails. It is not the main window.
/// </summary>
public sealed class SplashWindow : Window
{
    readonly SplashViewModel _model;
    readonly TextBlock _stage;
    readonly StackPanel _error;
    readonly StackPanel _loading;
    readonly TextBlock _errorTitle = new() { FontSize = 18, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock _message = new() { TextWrapping = TextWrapping.Wrap };
    readonly TextBlock _reference = new() { Opacity = 0.7, FontSize = 12 };

    public SplashWindow(SplashViewModel model)
    {
        _model = model;
        Title = model.Name;
        Width = 520;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        CloseButton = new Button { Content = model.CloseLabel, HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true };
        CloseButton.Click += (_, _) => Close();

        _stage = new TextBlock { Text = model.StageText, Opacity = 0.8 };
        _error = new StackPanel { Spacing = 12, IsVisible = false, Children = { _errorTitle, _message, _reference, CloseButton } };
        _loading = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = model.Name, FontSize = 28, FontWeight = FontWeight.Bold },
                _stage,
                new ProgressBar { IsIndeterminate = true },
            },
        };

        Content = new StackPanel { Margin = new Thickness(24), Children = { _loading, _error } };
        model.PropertyChanged += OnModelChanged;
        Refresh();
    }

    public Button CloseButton { get; }

    void OnModelChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    void Refresh()
    {
        _stage.Text = _model.StageText;
        _errorTitle.Text = _model.ErrorTitle;
        _message.Text = _model.Message;
        _reference.Text = _model.Reference;
        _error.IsVisible = _model.IsError;
        _loading.IsVisible = !_model.IsError;
        if (_model.IsError)
        {
            CloseButton.Focus();
        }
    }
}
