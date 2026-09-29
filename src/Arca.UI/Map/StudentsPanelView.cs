// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Students;
using Arca.UI.Actions;
using Arca.UI.Assigning;
using Arca.UI.Common;
using Arca.UI.Lists;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Arca.UI.Map;

/// <summary>
/// The panel of students without a locker: the title with the count, a search box, and the list. A student can be dragged from
/// it onto a free locker of the map, or chosen and put in the chosen locker from the menu (right button or menu key) or with
/// Enter, which do the same thing with the same checks and warnings.
/// </summary>
public sealed class StudentsPanelView : UserControl
{
    readonly StudentsWithoutLockerViewModel _model;
    readonly ILocalizer _localizer;
    readonly TextBlock _count;
    readonly ListBox _list;

    public StudentsPanelView(StudentsWithoutLockerViewModel model, AssignmentDropViewModel drop, AppAction assignToSelected, ILocalizer localizer)
    {
        _model = model;
        _localizer = localizer;
        _count = new TextBlock()
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        var search = new TextBox { PlaceholderText = localizer.Get("Shell.Students.Search") };
        search.TextChanged += (_, _) => model.FilterText = search.Text ?? string.Empty;

        _list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<StudentRow>((row, _) => row is null ? null : Row(row, drop), supportsRecycling: false),
        };
        _list.SelectionChanged += (_, _) => model.Select((_list.SelectedItem as StudentRow)?.Id);
        ActionControls.AttachContextMenu(_list, () => [assignToSelected]);
        _list.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter && assignToSelected.CanExecute(null))
            {
                assignToSelected.Execute(null);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);

        var body = new Grid();
        body.Children.Add(_list);
        body.Children.Add(new ListStateView(model.State));
        var header = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        header.Children.Add(ThemedText.Title(localizer.Get("Shell.Students.Title")));
        header.Children.Add(_count);
        header.Children.Add(search);
        var layout = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        layout.Children.Add(header);
        layout.Children.Add(body);
        Content = new Border { Child = layout }
            .Themed(Border.BackgroundProperty, ArcaResourceKeys.Surface)
            .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium);

        model.PropertyChanged += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>The list of students, so a test or the keyboard can reach it.</summary>
    public ListBox List => _list;

    /// <summary>What the count says.</summary>
    public string CountText => _count.Text ?? string.Empty;

    void Refresh()
    {
        _count.Text = _localizer.Get("Shell.Students.Count", _model.Count);
        _count.IsVisible = !_model.NoActiveYear;
        _list.ItemsSource = _model.Students;
    }

    static Border Row(StudentRow student, AssignmentDropViewModel drop)
    {
        var name = new TextBlock { Text = $"{student.LastName}, {student.FirstName}" }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        var group = string.Join(' ', new[] { student.LevelName, student.GroupName }.Where(x => !string.IsNullOrEmpty(x)));
        var details = new TextBlock { Text = group }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeSmall);
        var stack = new StackPanel();
        stack.Children.Add(name);
        stack.Children.Add(details);
        // A background, so the whole row can be grabbed and not only its text.
        var row = new Border { Child = stack }
            .Themed(Border.BackgroundProperty, ArcaResourceKeys.Surface)
            .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingSmall);
        StudentDragSource.Attach(row, () => student.Id, drop);
        return row;
    }
}
