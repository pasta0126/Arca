// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.ComponentModel;
using Arca.UI.Common;
using Material.Icons;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Arca.UI.Access;

/// <summary>
/// Draws an <see cref="AccessFormViewModel"/>. It only shows what the model says and forwards the buttons and keys:
/// Enter submits (once, while busy it does nothing), Escape cancels, and everything works with the keyboard alone.
/// </summary>
public sealed class AccessWindow : Window
{
    readonly AccessFormViewModel _model;
    readonly StackPanel _hints = new() { Spacing = 2 };
    readonly TextBlock _error = ThemedText.Error();
    readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Opacity = 0.8 };
    readonly TextBlock _busy = new() { Opacity = 0.8 };
    readonly List<TextBox> _boxes = [];
    bool _wasBusy;

    public AccessWindow(AccessFormViewModel model)
    {
        AppIcon.ApplyTo(this);
        _model = model;
        Title = model.Title;
        Width = 520;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        PrimaryButton = new Button { Content = model.PrimaryLabel, IsDefault = true };
        CancelButton = new Button { Content = model.CancelLabel, IsCancel = true };
        PrimaryButton.Click += async (_, _) => await model.SubmitAsync();
        CancelButton.Click += (_, _) => model.Cancel();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        if (model.SecondaryLabel.Length > 0)
        {
            SecondaryButton = new Button { Content = model.SecondaryLabel };
            SecondaryButton.Click += (_, _) => model.ChooseSecondary();
            buttons.Children.Add(SecondaryButton);
        }

        buttons.Children.Add(CancelButton);
        buttons.Children.Add(PrimaryButton);

        var body = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        body.Children.Add(ThemedText.Title(model.Title));
        body.Children.Add(new TextBlock { Text = model.Intro, TextWrapping = TextWrapping.Wrap });
        if (model.Warning.Length > 0)
        {
            body.Children.Add(ThemedText.WarningNote(model.Warning));
        }

        if (model.Secret is { } secret)
        {
            body.Children.Add(BuildSecret(secret));
        }

        foreach (var field in model.Fields)
        {
            var box = new TextBox { Text = field.Text, PasswordChar = field.IsSecret ? '●' : '\0', AcceptsReturn = false };
            box.TextChanged += (_, _) => field.Text = box.Text ?? string.Empty;
            field.PropertyChanged += (_, _) =>
            {
                if (box.Text != field.Text)
                {
                    box.Text = field.Text;
                }
            };
            _boxes.Add(box);
            body.Children.Add(new StackPanel { Spacing = 4, Children = { new TextBlock { Text = field.Label }, field.IsSecret ? WithRevealButton(box, model) : box } });
        }

        body.Children.Add(_hints);
        body.Children.Add(_error);
        body.Children.Add(_busy);
        body.Children.Add(buttons);
        Content = body;

        model.PropertyChanged += OnModelChanged;
        model.Completion.ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(Close), TaskScheduler.Default);
        Refresh();
    }

    public Button PrimaryButton { get; }

    public Button CancelButton { get; }

    public Button? SecondaryButton { get; }

    /// <summary>The text boxes, in the order of the model's fields.</summary>
    public IReadOnlyList<TextBox> Boxes => _boxes;

    /// <summary>
    /// A password box with a small eye button beside it that shows what was typed while it is pressed in, and hides it again
    /// when pressed once more. The text is hidden every time the window opens, and the button is reachable with the keyboard.
    /// </summary>
    static Grid WithRevealButton(TextBox box, AccessFormViewModel model)
    {
        var eye = new Avalonia.Controls.Primitives.ToggleButton { Content = ThemedIcon.Create(MaterialIconKind.Eye), VerticalAlignment = VerticalAlignment.Stretch };
        ToolTip.SetTip(eye, model.ShowPasswordLabel);
        eye.IsCheckedChanged += (_, _) =>
        {
            box.RevealPassword = eye.IsChecked == true;
            eye.Content = ThemedIcon.Create(eye.IsChecked == true ? MaterialIconKind.EyeOff : MaterialIconKind.Eye); // shows what pressing it will do
            ToolTip.SetTip(eye, eye.IsChecked == true ? model.HidePasswordLabel : model.ShowPasswordLabel);
        };
        Grid.SetColumn(eye, 1);
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        row.Children.Add(box);
        row.Children.Add(eye);
        return row;
    }

    StackPanel BuildSecret(SecretDisplay secret)
    {
        var copy = new Button { Content = secret.CopyLabel };
        var print = new Button { Content = secret.PrintLabel };
        copy.Click += async (_, _) => await _model.CopyAsync();
        print.Click += async (_, _) => await _model.PrintAsync();
        return new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = secret.Label, Opacity = 0.8 },
                ThemedText.Monospace(secret.Formatted),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { copy, print } },
                _status,
            },
        };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        (_boxes.FirstOrDefault() as Control ?? PrimaryButton).Focus();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        _model.Cancel(); // closing with the window button is cancelling; a no-op if the form already ended
        _model.Sink?.Cleanup();
    }

    void OnModelChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    void Refresh()
    {
        _hints.Children.Clear();
        foreach (var line in _model.Hints)
        {
            _hints.Children.Add(new TextBlock { Text = line, Opacity = 0.8, TextWrapping = TextWrapping.Wrap });
        }

        _error.Text = _model.Error;
        _error.IsVisible = _model.Error.Length > 0;
        _status.Text = _model.Status;
        _status.IsVisible = _model.Status.Length > 0;
        _busy.Text = _model.IsBusy ? _model.BusyText : string.Empty;
        _busy.IsVisible = _model.IsBusy;
        PrimaryButton.IsEnabled = _model.CanSubmit;
        foreach (var box in _boxes)
        {
            box.IsEnabled = _model.CanSubmit;
        }

        if (_wasBusy && !_model.IsBusy)
        {
            // Disabling the boxes while checking took the focus away; give it back so the person can retype at once.
            (_boxes.FirstOrDefault() as Control ?? PrimaryButton).Focus();
        }

        _wasBusy = _model.IsBusy;
    }
}
