// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Arca.UI.Lists;

/// <summary>
/// A list of rows with a header that orders by column, a check box per row and the selected count on top of the total. Only
/// the rows in view are built, however many there are: the list is a ListBox, which virtualizes its items. Space marks or
/// unmarks the focused row; the marks belong to the model, by identity, not to the rows drawn.
/// </summary>
public sealed class VirtualizedListView<TRow, TKey> : UserControl
    where TKey : notnull
{
    /// <summary>The width of the column of check boxes, the same in the header and in every row so their columns line up.</summary>
    const double CheckColumnWidth = 40;

    readonly ListViewModel<TRow, TKey> _model;
    readonly double _checkWidth;
    readonly TextBlock _count;
    readonly ListBox _list;
    readonly Grid _header;

    /// <param name="showMarks">Whether each row has a check box for marking it. A list whose rows are only chosen to see their detail has none.</param>
    public VirtualizedListView(ListViewModel<TRow, TKey> model, bool showMarks = true)
    {
        _model = model;
        _checkWidth = showMarks ? CheckColumnWidth : 0;
        _header = BuildHeader();
        _count = new TextBlock()
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeSmall);
        _list = new ListBox
        {
            ItemsSource = model.Rows,
            ItemTemplate = new FuncDataTemplate<TRow>((row, _) => row is null ? null : Row(row), supportsRecycling: false),
        };
        // The rows have no padding of their own, so their columns line up with the ones of the header above.
        _list.Styles.Add(new Style(x => x.OfType<ListBoxItem>()) { Setters = { new Setter(Avalonia.Controls.Primitives.TemplatedControl.PaddingProperty, new Thickness(0)) } });
        _list.AddHandler(InputElement.KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        var layout = new DockPanel();
        DockPanel.SetDock(_header, Dock.Top);
        DockPanel.SetDock(_count, Dock.Bottom);
        layout.Children.Add(_header);
        layout.Children.Add(_count);
        layout.Children.Add(_list);
        Content = layout;

        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ListViewModel<TRow, TKey>.Rows))
            {
                _list.ItemsSource = model.Rows;
            }

            if (e.PropertyName is nameof(ListViewModel<TRow, TKey>.SortColumnId) or nameof(ListViewModel<TRow, TKey>.SortDescending))
            {
                RebuildHeader();
            }

            _count.Text = model.SelectionText;
        };
        _count.Text = model.SelectionText;
        _count.IsVisible = showMarks;
    }

    /// <summary>The list control, so a screen or a test can reach the rows and the focus.</summary>
    public ListBox List => _list;

    /// <summary>The header buttons, one per column.</summary>
    public IReadOnlyList<Button> HeaderButtons => [.. _header.Children.OfType<Button>()];

    /// <summary>The text under the list, "295 de 300".</summary>
    public string CountText => _count.Text ?? string.Empty;

    Grid BuildHeader()
    {
        var grid = new Grid();
        Fill(grid);
        return grid;
    }

    void RebuildHeader()
    {
        _header.Children.Clear();
        Fill(_header);
    }

    void Fill(Grid grid)
    {
        if (grid.ColumnDefinitions.Count == 0)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(_checkWidth)));
            foreach (var column in _model.Columns)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(column.Width, GridUnitType.Star)));
            }
        }

        for (var i = 0; i < _model.Columns.Count; i++)
        {
            var column = _model.Columns[i];
            var arrow = _model.SortColumnId == column.Id ? (_model.SortDescending ? " ▼" : " ▲") : string.Empty;
            var button = new Button { Content = column.Header + arrow, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
            var id = column.Id;
            button.Click += (_, _) => _model.SortBy(id);
            Grid.SetColumn(button, i + 1);
            grid.Children.Add(button);
        }
    }

    Grid Row(TRow row)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(_checkWidth)));
        foreach (var column in _model.Columns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(column.Width, GridUnitType.Star)));
        }

        var check = new CheckBox { IsChecked = _model.IsSelected(row), IsTabStop = false, IsVisible = _checkWidth > 0 };
        check.IsCheckedChanged += (_, _) => _model.SetSelected(row, check.IsChecked == true);
        grid.Children.Add(check);
        for (var i = 0; i < _model.Columns.Count; i++)
        {
            var text = new TextBlock { Text = _model.Columns[i].Text(row), VerticalAlignment = VerticalAlignment.Center, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis }
                .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
                .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
            Grid.SetColumn(text, i + 1);
            grid.Children.Add(text);
        }

        // If the mark changes some other way, the row draws it, and only while it is on screen.
        void OnSelectionChanged(object? sender, EventArgs e) => check.IsChecked = _model.IsSelected(row);
        grid.AttachedToVisualTree += (_, _) => _model.SelectionChanged += OnSelectionChanged;
        grid.DetachedFromVisualTree += (_, _) => _model.SelectionChanged -= OnSelectionChanged;
        return grid;
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && _list.SelectedItem is TRow row)
        {
            _model.Toggle(row);
            e.Handled = true;
        }
    }
}
