// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Search;
using Arca.Domain.Common;
using Arca.UI.Assigning;
using Arca.UI.Common;
using Arca.UI.Layout;
using Arca.UI.Lists;
using Arca.UI.Preferences;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Material.Icons;

namespace Arca.UI.Lockers;

/// <summary>
/// The lockers of the Lockers section drawn as a map (pantalles-taquilles-i-zones, Vista de mapa de taquillas): the zones as sections that
/// fold, told apart with room between them, each with its lockers as cells that say their number and, by colour and icon, their status, with
/// the mark of debt on an occupied one. It is another view of the same model as the list: it draws the rows the filters leave, the row chosen
/// is the one chosen in the list, and the counters by status are filters. A cell is a drop destination for a student dragged from the panel.
/// </summary>
public sealed class LockersMapView : UserControl
{
    static readonly LockerStatusView[] _statuses =
        [LockerStatusView.Free, LockerStatusView.Occupied, LockerStatusView.Reserved, LockerStatusView.Broken, LockerStatusView.Maintenance];

    readonly LockersViewModel _model;
    readonly ILocalizer _localizer;
    readonly UiPreferencesSession _preferences;
    readonly AssignmentDropViewModel? _drop;
    readonly StackPanel _zones = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingLarge);
    readonly WrapPanel _chips = new() { Orientation = Orientation.Horizontal };
    readonly Dictionary<Guid, Button> _cells = [];
    readonly Dictionary<Guid, CollapsibleSectionViewModel> _sections = [];
    readonly Border _pickBanner;
    readonly TextBlock _pickText = new();

    public LockersMapView(LockersViewModel model, ILocalizer localizer, UiPreferencesSession preferences, AssignmentDropViewModel? drop = null, Control? filters = null)
    {
        _model = model;
        _localizer = localizer;
        _preferences = preferences;
        _drop = drop;
        Toolbar = new ListToolbarView<LockerListRow, Guid>(model.Lockers, localizer, filters);
        _pickBanner = BuildPickBanner();
        _chips.ThemedThickness(MarginProperty, ArcaResourceKeys.SpacingMedium);

        var top = new StackPanel();
        top.Children.Add(Toolbar);
        top.Children.Add(_chips);
        top.Children.Add(_pickBanner);
        DockPanel.SetDock(top, Dock.Top);
        var body = new Grid();
        body.Children.Add(new ScrollViewer { Content = _zones });
        body.Children.Add(new ListStateView(model.Lockers.State));
        var layout = new DockPanel();
        layout.Children.Add(top);
        layout.Children.Add(body);
        Content = layout;

        model.Lockers.List.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Lists.ListViewModel<LockerListRow, Guid>.Rows))
            {
                RebuildZones();
            }
        };
        model.Lockers.CurrentChanged += (_, _) => MarkCells();
        model.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(LockersViewModel.Counters):
                case nameof(LockersViewModel.StatusFilter):
                    RebuildChips();
                    break;
                case nameof(LockersViewModel.Picking):
                    RefreshPickBanner();
                    break;
                case nameof(LockersViewModel.HighlightedLockerId):
                    Unfold();
                    MarkCells();
                    break;
                default:
                    break;
            }
        };
        RebuildChips();
        RebuildZones(); // the lockers may already be loaded when the view is built
    }

    /// <summary>The search, filters, Reset button, labels and count above the map.</summary>
    public ListToolbarView<LockerListRow, Guid> Toolbar { get; }

    /// <summary>The cell of each locker drawn now, so a test can press it.</summary>
    public IReadOnlyDictionary<Guid, Button> Cells => _cells;

    /// <summary>The counters by status, each one a filter when pressed.</summary>
    public IReadOnlyList<ToggleButton> Chips => [.. _chips.Children.OfType<ToggleButton>()];

    public Border PickBanner => _pickBanner;

    /// <summary>The sections of the zones drawn now, by zone, so a test can tell whether one is unfolded.</summary>
    public IReadOnlyDictionary<Guid, CollapsibleSectionViewModel> Sections => _sections;

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
            var chip = new ToggleButton { Content = row, IsChecked = _model.StatusFilter == status.ToString() }
                .Themed(TemplatedControl.BackgroundProperty, LockerStatusPresentation.BrushKey(status))
                .ThemedThickness(Layoutable.MarginProperty, ArcaResourceKeys.SpacingSmall);
            var chosen = status.ToString();
            chip.Click += (_, _) => _model.StatusFilter = _model.StatusFilter == chosen ? string.Empty : chosen;
            _chips.Children.Add(chip);
        }
    }

    void RebuildZones()
    {
        _zones.Children.Clear();
        _cells.Clear();
        _sections.Clear();
        var groups = _model.Lockers.List.Rows
            .Where(r => r.ZoneActive)
            .GroupBy(r => (r.ZoneId, r.ZoneName))
            .OrderBy(g => g.Key.ZoneName, TextComparer.Comparer);
        foreach (var group in groups)
        {
            var cells = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var locker in group)
            {
                var cell = Cell(locker);
                _cells[locker.Id] = cell;
                var host = new Border { Child = cell, BorderThickness = new Thickness(3) }; // the outline of a drop goes here, around the cell
                if (_drop is not null)
                {
                    var id = locker.Id;
                    LockerDropTarget.Attach(host, () => id, _drop);
                }

                cells.Children.Add(host);
            }

            var inZone = _model.Lockers.List.AllRows.Where(r => r.ZoneId == group.Key.ZoneId && r.Status != LockerStatusView.Retired).ToList();
            var summary = _localizer.Get("Shell.Map.ZoneSummary", inZone.Count, inZone.Count(r => r.Status == LockerStatusView.Free));
            var section = new CollapsibleSectionViewModel("zone:" + group.Key.ZoneId.ToString("N"), group.Key.ZoneName, () => summary, _preferences);
            _sections[group.Key.ZoneId] = section;
            _zones.Children.Add(new CollapsibleSectionView(section, cells));
        }

        MarkCells();
    }

    /// <summary>Opens the zone of the locker the search chose, so it is seen.</summary>
    void Unfold()
    {
        if (_model.HighlightedLockerId is { } id && _model.Find(id) is { } row && _sections.TryGetValue(row.ZoneId, out var section))
        {
            section.IsExpanded = true;
        }
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
        return new Border { Child = row, IsVisible = false }
            .Themed(Border.BackgroundProperty, ArcaResourceKeys.Warning)
            .ThemedThickness(Border.PaddingProperty, ArcaResourceKeys.SpacingMedium);
    }

    void RefreshPickBanner()
    {
        _pickBanner.IsVisible = _model.Picking is not null;
        _pickText.Text = _model.Picking is { } picking ? _localizer.Get("Shell.Pick.Banner", picking.StudentName) : string.Empty;
    }

    Button Cell(LockerListRow locker)
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
        var id = locker.Id;
        button.Click += (_, _) => _model.SelectLocker(id);
        ToolTip.SetTip(button, TipOf(locker));
        return button;
    }

    string TipOf(LockerListRow locker)
    {
        var tip = _localizer.Get("Shell.Map.CellTip", locker.Number, LockerStatusPresentation.Text(locker.Status, _localizer));
        if (locker.StudentName is not null)
        {
            tip = _localizer.Get("Shell.Map.CellTipStudent", tip, locker.StudentName);
        }

        return locker.HasDebt ? _localizer.Get("Shell.Map.CellTipStudent", tip, _localizer.Get("Shell.Map.Debt")) : tip;
    }

    void MarkCells()
    {
        _model.Lockers.TryGetSelectedKey(out var selected);
        var hasSelection = _model.Lockers.HasSelection;
        foreach (var (id, cell) in _cells)
        {
            if (hasSelection && id == selected)
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
