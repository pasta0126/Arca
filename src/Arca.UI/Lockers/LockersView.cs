// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Search;
using Arca.UI.Actions;
using Arca.UI.Map;
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
    /// <summary>The filters of the lockers, as controls of their own: the list and the map each draw theirs over the same model and they follow each other.</summary>
    static WrapPanel BuildFilters(LockersViewModel model, ILocalizer localizer)
    {
        var retired = new CheckBox { Content = localizer.Get("Lockers.Label.IncludeRetired") };
        var number = new TextBox { PlaceholderText = localizer.Get("Lockers.Label.Number"), Width = 90 };
        var zone = Filter(model.ZoneOptions, () => model.ZoneFilter, v => model.ZoneFilter = v);
        var status = Filter(model.StatusOptions, () => model.StatusFilter, v => model.StatusFilter = v);
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LockersViewModel.ZoneOptions))
            {
                zone.ItemsSource = model.ZoneOptions;
                zone.SelectedItem = model.ZoneOptions.FirstOrDefault(o => o.Id == model.ZoneFilter) ?? (model.ZoneOptions.Count > 0 ? model.ZoneOptions[0] : null);
            }

            if (e.PropertyName == nameof(LockersViewModel.ZoneFilter))
            {
                zone.SelectedItem = model.ZoneOptions.FirstOrDefault(o => o.Id == model.ZoneFilter);
            }

            if (e.PropertyName == nameof(LockersViewModel.IncludeRetired))
            {
                retired.IsChecked = model.IncludeRetired;
            }

            if (e.PropertyName == nameof(LockersViewModel.NumberFilter) && number.Text != model.NumberFilter)
            {
                number.Text = model.NumberFilter;
            }

            if (e.PropertyName == nameof(LockersViewModel.StatusFilter))
            {
                status.SelectedItem = model.StatusOptions.FirstOrDefault(o => o.Id == model.StatusFilter);
            }
        };
        number.TextChanged += (_, _) => model.NumberFilter = number.Text ?? string.Empty;
        retired.IsCheckedChanged += (_, _) => model.IncludeRetired = retired.IsChecked == true;
        var filters = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        filters.Children.Add(zone);
        filters.Children.Add(status);
        filters.Children.Add(number);
        filters.Children.Add(retired);
        return filters;
    }

    /// <summary>The list view of the lockers: title and actions, the list with its filters and counters, and the detail of the locker chosen.</summary>
    public static ScreenView Create(LockersViewModel model, ILocalizer localizer)
    {
        var filters = BuildFilters(model, localizer);
        var list = new ScreenListView<LockerListRow, Guid>(model.Lockers, localizer, filters);
        var counters = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary);
        counters.Bind(TextBlock.TextProperty, new Binding(nameof(LockersViewModel.CountersText)) { Source = model });
        var left = new DockPanel();
        DockPanel.SetDock(counters, Dock.Bottom);
        left.Children.Add(counters);
        left.Children.Add(list);

        var screen = new ScreenView(localizer.Get("Shell.Screen.Lockers"), model.ListActions, left, new LockerDetailPanel(model, localizer), selection: model.Lockers);
        IDisposable? shortcut = null;
        screen.AttachedToVisualTree += (_, _) =>
        {
            shortcut = model.StandardNew.Attach(() => model.NewLocker.Execute(null)); // Control or Command + N, only while this view is open
            _ = model.LoadAsync();
        };
        screen.DetachedFromVisualTree += (_, _) => shortcut?.Dispose();
        return screen;
    }

    /// <summary>
    /// The map view of the lockers: the same model, filters, selection and detail as the list, and beside the detail the students without a
    /// locker to drag onto a free one.
    /// </summary>
    public static ScreenView CreateMap(LockersViewModel model, LockersMapModel map, ILocalizer localizer, Arca.UI.Preferences.UiPreferencesSession preferences)
    {
        var view = new LockersMapView(model, localizer, preferences, map.Drop, BuildFilters(model, localizer));
        var side = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        var detail = new LockerDetailPanel(model, localizer);
        var students = new StudentsPanelView(map.Students, map.Drop, map.AssignToSelected, localizer);
        Grid.SetRow(students, 1);
        side.Children.Add(detail);
        side.Children.Add(students);

        var screen = new ScreenView(localizer.Get("Shell.Screen.LockerMap"), model.MapActions, view, side, selection: model.Lockers);
        IDisposable? shortcut = null;
        screen.AttachedToVisualTree += (_, _) =>
        {
            shortcut = model.StandardNew.Attach(() => model.NewLocker.Execute(null)); // Control or Command + N, only while this view is open
            _ = LoadAsync(model, map);
        };
        screen.DetachedFromVisualTree += (_, _) => shortcut?.Dispose();
        return screen;
    }

    static async Task LoadAsync(LockersViewModel model, LockersMapModel map) => await Task.WhenAll(model.LoadAsync(), map.LoadAsync());

    static ComboBox Filter(IReadOnlyList<FormOption> options, Func<string> current, Action<string> set)
    {
        var box = new ComboBox { ItemsSource = options, MinWidth = 150 };
        box.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<FormOption>((o, _) => new TextBlock { Text = o?.Label });
        box.SelectedItem = options.FirstOrDefault(o => o.Id == current()) ?? (options.Count > 0 ? options[0] : null);
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
