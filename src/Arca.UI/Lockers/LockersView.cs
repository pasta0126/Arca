// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Search;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Arca.UI.Lockers;

/// <summary>Builds the Lockers view: title and actions, the list with its filters and counters, and the detail of the locker chosen.</summary>
public static class LockersView
{
    public static ScreenView Create(LockersViewModel model, ILocalizer localizer)
    {
        var zone = Filter(model.ZoneOptions, () => model.ZoneFilter, v => model.ZoneFilter = v);
        var status = Filter(model.StatusOptions, () => model.StatusFilter, v => model.StatusFilter = v);
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LockersViewModel.ZoneOptions))
            {
                zone.ItemsSource = model.ZoneOptions;
                zone.SelectedItem = model.ZoneOptions.FirstOrDefault(o => o.Id == model.ZoneFilter) ?? model.ZoneOptions[0];
            }

            if (e.PropertyName == nameof(LockersViewModel.ZoneFilter))
            {
                zone.SelectedItem = model.ZoneOptions.FirstOrDefault(o => o.Id == model.ZoneFilter);
            }

            if (e.PropertyName == nameof(LockersViewModel.StatusFilter))
            {
                status.SelectedItem = model.StatusOptions.FirstOrDefault(o => o.Id == model.StatusFilter);
            }
        };
        var number = new TextBox { PlaceholderText = localizer.Get("Lockers.Label.Number"), Width = 90 };
        number.TextChanged += (_, _) => model.NumberFilter = number.Text ?? string.Empty;
        var retired = new CheckBox { Content = localizer.Get("Lockers.Label.IncludeRetired") };
        retired.IsCheckedChanged += (_, _) => model.IncludeRetired = retired.IsChecked == true;
        var filters = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        filters.Children.Add(zone);
        filters.Children.Add(status);
        filters.Children.Add(number);
        filters.Children.Add(retired);

        var list = new ScreenListView<LockerListRow, Guid>(model.Lockers, localizer, filters);
        var counters = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary);
        counters.Bind(TextBlock.TextProperty, new Binding(nameof(LockersViewModel.CountersText)) { Source = model });
        var left = new DockPanel();
        DockPanel.SetDock(counters, Dock.Bottom);
        left.Children.Add(counters);
        left.Children.Add(list);

        var screen = new ScreenView(localizer.Get("Shell.Screen.Lockers"), model.MainActions, left, new LockerDetailPanel(model, localizer));
        IDisposable? shortcut = null;
        screen.AttachedToVisualTree += (_, _) =>
        {
            shortcut = model.StandardNew.Attach(() => model.NewLocker.Execute(null)); // Control or Command + N, only while this view is open
            _ = model.LoadAsync();
        };
        screen.DetachedFromVisualTree += (_, _) => shortcut?.Dispose();
        return screen;
    }

    static ComboBox Filter(IReadOnlyList<FormOption> options, Func<string> current, Action<string> set)
    {
        var box = new ComboBox { ItemsSource = options, MinWidth = 150 };
        box.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<FormOption>((o, _) => new TextBlock { Text = o?.Label });
        box.SelectedItem = options.FirstOrDefault(o => o.Id == current()) ?? options[0];
        box.SelectionChanged += (_, _) =>
        {
            if (box.SelectedItem is FormOption option)
            {
                set(option.Id);
            }
        };
        return box;
    }
}
