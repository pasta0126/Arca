// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace Arca.UI.Identity;

/// <summary>
/// The identity and the theme in the Settings section: the name, the logo and the accent of the centre with a preview of the header
/// before saving, and the choice of theme, which applies at once. Built from the models, with no rule of its own.
/// </summary>
public static class IdentitySettingsView
{
    public static Control Identity(IdentityViewModel model, ILocalizer localizer)
    {
        var name = new TextBox { PlaceholderText = localizer.Get("Identity.Label.Name"), MaxLength = 100 };
        name.Bind(TextBox.TextProperty, new Binding(nameof(IdentityViewModel.Name)) { Source = model, Mode = BindingMode.TwoWay });

        var accent = new TextBox { PlaceholderText = "#RRGGBB", Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
        accent.Bind(TextBox.TextProperty, new Binding(nameof(IdentityViewModel.AccentText)) { Source = model, Mode = BindingMode.TwoWay });
        var accentError = ThemedText.Error();
        accentError.Bind(TextBlock.TextProperty, new Binding(nameof(IdentityViewModel.AccentError)) { Source = model });
        accentError.Bind(Visual.IsVisibleProperty, new Binding(nameof(IdentityViewModel.AccentError)) { Source = model, Converter = Avalonia.Data.Converters.StringConverters.IsNotNullOrEmpty });
        var swatches = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 6, LineSpacing = 6 };
        foreach (var colour in AccentSwatches.Palette)
        {
            var swatch = new Button { Background = AccentSwatches.Brush(colour), Width = 32, Height = 32, Tag = colour };
            ToolTip.SetTip(swatch, colour);
            swatch.Click += (_, _) => model.AccentText = colour;
            swatches.Children.Add(swatch);
        }

        var preview = Preview(model, localizer);
        var actions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        foreach (var action in new[] { model.ChooseLogoAction, model.RemoveLogoAction, model.ResetAccentAction, model.SaveAction })
        {
            actions.Children.Add(ActionControls.Button(action));
        }

        var body = new StackPanel { Spacing = 10 };
        body.Children.Add(Label(localizer.Get("Identity.Label.Name"), name));
        body.Children.Add(Label(localizer.Get("Identity.Label.Accent"), accent));
        body.Children.Add(accentError);
        body.Children.Add(swatches);
        body.Children.Add(ThemedText.Small(localizer.Get("Identity.Label.Preview")));
        body.Children.Add(preview);
        body.Children.Add(new WorkIndicator(model, localizer));
        body.Children.Add(actions);
        return body;
    }

    static StackPanel Label(string text, Control field)
    {
        var row = new StackPanel { Spacing = 4 };
        row.Children.Add(new TextBlock { Text = text });
        row.Children.Add(field);
        return row;
    }

    /// <summary>A small header as it will look: logo, name and a button of the accent, rebuilt whenever the form changes.</summary>
    static Border Preview(IdentityViewModel model, ILocalizer localizer)
    {
        var holder = new Border { BorderThickness = new Thickness(1), Padding = new Thickness(12), CornerRadius = new CornerRadius(4) };
        holder.Bind(Border.BorderBrushProperty, holder.GetResourceObservable(ArcaResourceKeys.Border));
        void Rebuild()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            if (model.PreviewLogo is { } logo && LogoImages.TryDecode(logo) is { } bitmap)
            {
                row.Children.Add(new Image { Source = bitmap, Height = 32, VerticalAlignment = VerticalAlignment.Center });
            }

            row.Children.Add(ThemedText.Title(model.PreviewName));
            var colours = AccentTheme.For(model.PreviewAccent, PaletteColors.Light, dark: false);
            var sample = new Button { Content = localizer.Get("Identity.Label.PreviewButton") };
            AccentSwatches.Sample(sample, colours);
            row.Children.Add(sample);
            holder.Child = row;
        }

        model.PropertyChanged += (_, _) => Rebuild();
        Rebuild();
        return holder;
    }

    public static Control Theme(ThemeSettingsViewModel model, ILocalizer localizer)
    {
        var panel = new StackPanel { Spacing = 6 };
        foreach (var choice in model.Choices)
        {
            var key = choice switch
            {
                ThemeChoice.Dark => "Identity.Theme.Dark",
                ThemeChoice.System => "Identity.Theme.System",
                _ => "Identity.Theme.Light",
            };
            var radio = new RadioButton { Content = localizer.Get(key), GroupName = "ArcaTheme", IsChecked = model.Choice == choice, Tag = choice };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                {
                    model.Choice = choice;
                }
            };
            panel.Children.Add(radio);
        }

        return panel;
    }

    sealed class WorkIndicator : ContentControl
    {
        public WorkIndicator(IdentityViewModel model, ILocalizer localizer) =>
            Content = new Arca.UI.Commands.WorkIndicatorView(model.Work, localizer);
    }
}
