// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Search;
using Arca.UI.Assigning;
using Arca.UI.Common;
using Arca.UI.Layout;
using Arca.UI.Lists;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;

namespace Arca.UI.Map;

/// <summary>
/// The map of lockers by zone: a bar of counters that also filter by status, a filter by zone, and each zone as a folding
/// section holding its lockers in a grid, by number. Each locker shows its number, its status by colour, icon and word (in its
/// tooltip), and a mark when the student who holds it owes something. The one whose detail is open and the one found by the
/// search are drawn with an outline.
/// </summary>
public sealed class LockerMapView : UserControl
{
    static readonly LockerStatusView[] _statuses =
        [LockerStatusView.Free, LockerStatusView.Occupied, LockerStatusView.Reserved, LockerStatusView.Broken, LockerStatusView.Maintenance];

    readonly LockerMapViewModel _model;
    readonly ILocalizer _localizer;
    readonly AssignmentDropViewModel? _drop;
    readonly StackPanel _zones = new();
    readonly WrapPanel _chips = new() { Orientation = Orientation.Horizontal };
    readonly ComboBox _zoneFilter = new();
    readonly Dictionary<Guid, Button> _cells = [];
    readonly Border _pickBanner;
    readonly TextBlock _pickText = new();
    bool _syncing;

    /// <param name="drop">What dragging a student over a locker does; without it the lockers accept no drops.</param>
    public LockerMapView(LockerMapViewModel model, ILocalizer localizer, AssignmentDropViewModel? drop = null)
    {
        _model = model;
        _localizer = localizer;
        _drop = drop;
        _zoneFilter.SelectionChanged += (_, _) =>
        {
            if (!_syncing)
            {
                _model.ZoneFilter = _zoneFilter.SelectedIndex > 0 ? _model.ZoneNames[_zoneFilter.SelectedIndex - 1].ZoneId : null;
            }
        };

        var bar = new DockPanel().ThemedThickness(MarginProperty, ArcaResourceKeys.SpacingMedium);
        DockPanel.SetDock(_zoneFilter, Dock.Right);
        bar.Children.Add(_zoneFilter);
        bar.Children.Add(_chips);

        _pickBanner = BuildPickBanner();
        DockPanel.SetDock(_pickBanner, Dock.Top);
        var body = new Grid();
        body.Children.Add(new ScrollViewer { Content = _zones });
        body.Children.Add(new ListStateView(model.State));
        var layout = new DockPanel();
        DockPanel.SetDock(bar, Dock.Top);
        layout.Children.Add(bar);
        layout.Children.Add(_pickBanner);
        layout.Children.Add(body);
        Content = layout;

        model.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(LockerMapViewModel.Zones):
                    RebuildZones();
                    break;
                case nameof(LockerMapViewModel.Counters):
                case nameof(LockerMapViewModel.StatusFilter):
                    RebuildChips();
                    break;
                case nameof(LockerMapViewModel.ZoneNames):
                    RebuildZoneFilter();
                    break;
                case nameof(LockerMapViewModel.ZoneFilter):
                    SyncZoneSelection();
                    break;
                case nameof(LockerMapViewModel.Picking):
                    RefreshPickBanner();
                    break;
                case nameof(LockerMapViewModel.SelectedLockerId):
                case nameof(LockerMapViewModel.HighlightedLockerId):
                    MarkCells();
                    break;
            }
        };
        RebuildZoneFilter();
        RebuildChips();
        RebuildZones(); // the map may already be loaded when the view is built
    }

    /// <summary>The locker cells on screen, by locker, so a test or the detail can reach them.</summary>
    public IReadOnlyDictionary<Guid, Button> Cells => _cells;

    /// <summary>The buttons of the counters, one per status, in order.</summary>
    public IReadOnlyList<ToggleButton> Chips => [.. _chips.Children.OfType<ToggleButton>()];

    /// <summary>The filter by zone.</summary>
    public ComboBox ZoneFilter => _zoneFilter;

    void RebuildChips()
    {
        _chips.Children.Clear();
        var counters = _model.Counters;
        foreach (var status in _statuses)
        {
            var count = status switch
            {
                LockerStatusView.Free => counters.Free,
                LockerStatusView.Occupied => counters.Occupied,
                LockerStatusView.Reserved => counters.Reserved,
                LockerStatusView.Broken => counters.Broken,
                _ => counters.Maintenance,
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
            row.Children.Add(ThemedIcon.Create(LockerStatusPresentation.Icon(status), 16));
            row.Children.Add(new TextBlock { Text = _localizer.Get("Shell.Map.Counter", LockerStatusPresentation.Text(status, _localizer), count), VerticalAlignment = VerticalAlignment.Center });
            var chip = new ToggleButton { Content = row, IsChecked = _model.StatusFilter == status }
                .Themed(TemplatedControl.BackgroundProperty, LockerStatusPresentation.BrushKey(status))
                .ThemedThickness(Layoutable.MarginProperty, ArcaResourceKeys.SpacingSmall);
            var chosen = status;
            chip.Click += (_, _) => _model.StatusFilter = _model.StatusFilter == chosen ? null : chosen;
            _chips.Children.Add(chip);
        }
    }

    void RebuildZoneFilter()
    {
        _syncing = true;
        _zoneFilter.Items.Clear();
        _zoneFilter.Items.Add(_localizer.Get("Shell.Map.AllZones"));
        foreach (var (_, name) in _model.ZoneNames)
        {
            _zoneFilter.Items.Add(name);
        }

        _syncing = false;
        SyncZoneSelection();
    }

    /// <summary>Makes the drop-down show the zone the model filters by, when the filter was changed by code (clearing, revealing).</summary>
    void SyncZoneSelection()
    {
        var at = _model.ZoneFilter is { } id ? _model.ZoneNames.ToList().FindIndex(z => z.ZoneId == id) + 1 : 0;
        _syncing = true;
        _zoneFilter.SelectedIndex = Math.Max(0, at);
        _syncing = false;
    }

    void RebuildZones()
    {
        _zones.Children.Clear();
        _cells.Clear();
        foreach (var zone in _model.Zones)
        {
            var cells = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var locker in zone.Visible)
            {
                var cell = Cell(locker);
                _cells[locker.LockerId] = cell;
                var host = new Border { Child = cell, BorderThickness = new Thickness(3) }; // the outline of a drop goes here, around the cell
                if (_drop is not null)
                {
                    var id = locker.LockerId;
                    LockerDropTarget.Attach(host, () => id, _drop);
                }

                cells.Children.Add(host);
            }

            _zones.Children.Add(new CollapsibleSectionView(zone.Section, cells));
        }

        MarkCells();
    }

    Border BuildPickBanner()
    {
        _pickText.Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.OnSemantic).Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        _pickText.VerticalAlignment = VerticalAlignment.Center;
        var cancel = new Button { Content = _localizer.Get("Shell.Pick.Cancel") };
        cancel.Click += (_, _) => _model.CancelPick();
        var row = new DockPanel();
        DockPanel.SetDock(cancel, Dock.Right);
        row.Children.Add(cancel);
        row.Children.Add(_pickText);
        var banner = new Border { Child = row, IsVisible = false }
            .Themed(Border.BackgroundProperty, ArcaResourceKeys.Warning)
            .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium);
        return banner;
    }

    /// <summary>Tells the person, while changing a student's locker, to choose the new one on the map.</summary>
    void RefreshPickBanner()
    {
        _pickBanner.IsVisible = _model.Picking is not null;
        _pickText.Text = _model.Picking is { } picking ? _localizer.Get("Shell.Pick.Banner", picking.StudentName) : string.Empty;
    }

    /// <summary>The banner that asks for the new locker, so a test can see whether it is on.</summary>
    public Border PickBanner => _pickBanner;

    Button Cell(MapLocker locker)
    {
        var grid = new Grid { Width = 68, Height = 48 };
        grid.Children.Add(new TextBlock { Text = locker.Number.ToString(System.Globalization.CultureInfo.CurrentCulture), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.SemiBold }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.OnSemantic)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody));
        var status = ThemedIcon.Create(LockerStatusPresentation.Icon(locker.Status), 14);
        status.HorizontalAlignment = HorizontalAlignment.Right;
        status.VerticalAlignment = VerticalAlignment.Top;
        grid.Children.Add(status);
        if (locker.HasDebt)
        {
            var debt = ThemedIcon.Create(LockerStatusPresentation.DebtIcon, 14);
            debt.HorizontalAlignment = HorizontalAlignment.Left;
            debt.VerticalAlignment = VerticalAlignment.Bottom;
            grid.Children.Add(debt);
        }

        var button = new Button { Content = grid, Padding = new Thickness(2), BorderThickness = new Thickness(3) }
            .Themed(TemplatedControl.BackgroundProperty, LockerStatusPresentation.BrushKey(locker.Status))
            .ThemedThickness(Layoutable.MarginProperty, ArcaResourceKeys.SpacingSmall);
        var id = locker.LockerId;
        button.Click += (_, _) => _model.Select(id);
        ToolTip.SetTip(button, TipOf(locker));
        return button;
    }

    string TipOf(MapLocker locker)
    {
        var tip = _localizer.Get("Shell.Map.CellTip", locker.Number, LockerStatusPresentation.Text(locker.Status, _localizer));
        if (locker.StudentName is not null)
        {
            tip = _localizer.Get("Shell.Map.CellTipStudent", tip, locker.StudentName);
        }

        return locker.HasDebt ? _localizer.Get("Shell.Map.CellTipStudent", tip, _localizer.Get("Shell.Map.Debt")) : tip;
    }

    /// <summary>Outlines the cell whose detail is open and the one the search found.</summary>
    void MarkCells()
    {
        foreach (var (id, cell) in _cells)
        {
            if (id == _model.SelectedLockerId)
            {
                cell.Themed(TemplatedControl.BorderBrushProperty, ArcaResourceKeys.Focus);
            }
            else if (id == _model.HighlightedLockerId)
            {
                cell.Themed(TemplatedControl.BorderBrushProperty, ArcaResourceKeys.Accent);
            }
            else
            {
                // Unmarked: the outline is the colour of the cell itself, so marking one does not move the others.
                cell.Themed(TemplatedControl.BorderBrushProperty, LockerStatusPresentation.BrushKey(_model.Find(id)?.Status ?? LockerStatusView.Free));
            }
        }
    }
}
