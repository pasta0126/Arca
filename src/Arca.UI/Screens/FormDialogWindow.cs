// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Commands;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;

namespace Arca.UI.Screens;

/// <summary>
/// A modal window for a form (pantalles-de-domini, D3): its fields with the error of each next to it, a note, and Save and Cancel.
/// Saving does not close it unless it succeeds, and Escape cancels. What was written stays while an error is shown.
/// </summary>
public sealed class FormDialogWindow : Window
{
    readonly IFormModel _model;

    public FormDialogWindow(IFormModel model, ILocalizer localizer)
    {
        _model = model;
        Title = model.Title;
        Width = 460;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var body = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        body.Children.Add(ThemedText.Title(model.Title));
        foreach (var field in model.Fields)
        {
            body.Children.Add(Row(field));
        }

        var note = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap }.Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary);
        note.Bind(TextBlock.TextProperty, new Binding(nameof(IFormModel.Note)) { Source = model });
        note.Bind(IsVisibleProperty, new Binding(nameof(IFormModel.Note)) { Source = model, Converter = Avalonia.Data.Converters.StringConverters.IsNotNullOrEmpty });
        body.Children.Add(note);
        var summary = ThemedText.Error();
        summary.Bind(TextBlock.TextProperty, new Binding(nameof(IFormModel.Summary)) { Source = model });
        summary.Bind(IsVisibleProperty, new Binding(nameof(IFormModel.Summary)) { Source = model, Converter = Avalonia.Data.Converters.StringConverters.IsNotNullOrEmpty });
        body.Children.Add(summary);

        SaveButton = new Button { Content = model.SaveLabel, IsDefault = true, Command = model.SaveCommand };
        CancelButton = new Button { Content = localizer.Get("Common.Label.Cancel"), IsCancel = true };
        CancelButton.Click += (_, _) => Close(false);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }
            .Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        buttons.Children.Add(new WorkIndicatorView(model.Work, localizer));
        buttons.Children.Add(CancelButton);
        buttons.Children.Add(SaveButton);
        body.Children.Add(buttons);
        body.ThemedThickness(MarginProperty, ArcaResourceKeys.SpacingLarge);
        Content = body;
        model.Succeeded += OnSucceeded;
    }

    public Button SaveButton { get; }

    public Button CancelButton { get; }

    /// <summary>The text box of each field, by the name of the field, so a test can write in it.</summary>
    public Dictionary<string, TextBox> Boxes { get; } = [];

    /// <summary>The drop-down of each field that is picked from a list, by the name of the field.</summary>
    public Dictionary<string, ComboBox> Choices { get; } = [];

    void OnSucceeded(object? sender, EventArgs e)
    {
        if (!_model.StaysOpen)
        {
            Close(true);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _model.Succeeded -= OnSucceeded;
        base.OnClosed(e);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ((Control?)Boxes.Values.FirstOrDefault() ?? Choices.Values.FirstOrDefault())?.Focus();
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

    StackPanel Row(FormFieldModel field)
    {
        Control input;
        if (field.Options is { } options)
        {
            var choice = new ComboBox { ItemsSource = options, HorizontalAlignment = HorizontalAlignment.Stretch };
            choice.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<FormOption>((o, _) => new TextBlock { Text = o?.Label });
            choice.SelectedItem = options.FirstOrDefault(o => o.Id == field.Text);
            choice.SelectionChanged += (_, _) => field.Text = (choice.SelectedItem as FormOption)?.Id ?? string.Empty;
            Choices[field.Id] = choice;
            input = choice;
        }
        else
        {
            var box = new TextBox { PlaceholderText = field.Label };
            box.Bind(TextBox.TextProperty, new Binding(nameof(FormFieldModel.Text)) { Source = field, Mode = BindingMode.TwoWay });
            Boxes[field.Id] = box;
            input = box;
        }

        var error = ThemedText.Error();
        error.Bind(TextBlock.TextProperty, new Binding(nameof(FormFieldModel.Error)) { Source = field });
        error.Bind(IsVisibleProperty, new Binding(nameof(FormFieldModel.HasError)) { Source = field });
        var row = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        row.Children.Add(new TextBlock { Text = field.Label }.Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text));
        row.Children.Add(input);
        row.Children.Add(error);
        return row;
    }
}
