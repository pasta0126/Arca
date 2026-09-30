// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Students.ListStudentRows;
using Arca.UI.Actions;
using Arca.UI.Common;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Arca.UI.Students;

/// <summary>Builds the Students section: title and actions, the list with search, filters and counters, and the record of the student chosen.</summary>
public static class StudentsView
{
    public static ScreenView Create(StudentsViewModel model, ILocalizer localizer)
    {
        var retired = new CheckBox { Content = localizer.Get("Students.Label.IncludeRetired") };
        var level = Filter(model.LevelOptions, () => model.LevelFilter, v => model.LevelFilter = v);
        var group = Filter(model.GroupOptions, () => model.GroupFilter, v => model.GroupFilter = v);
        var locker = Filter(model.LockerOptions, () => model.LockerFilter, v => model.LockerFilter = v);
        var payment = Filter(model.PaymentOptions, () => model.PaymentFilter, v => model.PaymentFilter = v);
        model.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(StudentsViewModel.LevelOptions):
                    level.ItemsSource = model.LevelOptions;
                    level.SelectedItem = model.LevelOptions.FirstOrDefault(o => o.Id == model.LevelFilter) ?? (model.LevelOptions.Count > 0 ? model.LevelOptions[0] : null);
                    break;
                case nameof(StudentsViewModel.GroupOptions):
                    group.ItemsSource = model.GroupOptions;
                    group.SelectedItem = model.GroupOptions.FirstOrDefault(o => o.Id == model.GroupFilter) ?? (model.GroupOptions.Count > 0 ? model.GroupOptions[0] : null);
                    break;
                case nameof(StudentsViewModel.LevelFilter):
                    level.SelectedItem = model.LevelOptions.FirstOrDefault(o => o.Id == model.LevelFilter);
                    break;
                case nameof(StudentsViewModel.GroupFilter):
                    group.SelectedItem = model.GroupOptions.FirstOrDefault(o => o.Id == model.GroupFilter);
                    break;
                case nameof(StudentsViewModel.LockerFilter):
                    locker.SelectedItem = model.LockerOptions.FirstOrDefault(o => o.Id == model.LockerFilter);
                    break;
                case nameof(StudentsViewModel.PaymentFilter):
                    payment.SelectedItem = model.PaymentOptions.FirstOrDefault(o => o.Id == model.PaymentFilter);
                    break;
                case nameof(StudentsViewModel.IncludeRetired):
                    retired.IsChecked = model.IncludeRetired;
                    break;
                default:
                    break;
            }
        };
        retired.IsCheckedChanged += (_, _) => model.IncludeRetired = retired.IsChecked == true;
        var filters = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 8 };
        filters.Children.Add(level);
        filters.Children.Add(group);
        filters.Children.Add(locker);
        filters.Children.Add(payment);
        filters.Children.Add(retired);

        var list = new ScreenListView<StudentListRow, Guid>(model.Students, localizer, filters);
        var counters = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap }.Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary);
        counters.Bind(TextBlock.TextProperty, new Binding(nameof(StudentsViewModel.CountersText)) { Source = model });
        var left = new DockPanel();
        DockPanel.SetDock(counters, Dock.Bottom);
        left.Children.Add(counters);
        left.Children.Add(list);

        var screen = new ScreenView(localizer.Get("Shell.Screen.Students"), model.MainActions, left, new StudentDetailPanel(model, localizer), selection: model.Students);
        IDisposable? shortcut = null;
        screen.AttachedToVisualTree += (_, _) =>
        {
            shortcut = model.StandardNew.Attach(() => model.NewStudent.Execute(null)); // Control or Command + N, only while this section is open
            _ = model.LoadAsync();
        };
        screen.DetachedFromVisualTree += (_, _) => shortcut?.Dispose();
        return screen;
    }

    static ComboBox Filter(IReadOnlyList<FormOption> options, Func<string> current, Action<string> set)
    {
        var box = new ComboBox { ItemsSource = options, MinWidth = 140 };
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
