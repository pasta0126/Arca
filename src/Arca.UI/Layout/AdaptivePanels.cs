// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;
using Avalonia.Controls;

namespace Arca.UI.Layout;

/// <summary>How two panels are arranged for the width available.</summary>
public enum PanelArrangement
{
    /// <summary>The main panel and the secondary one next to each other.</summary>
    SideBySide,

    /// <summary>The secondary panel under the main one, so the main one is never cut.</summary>
    Stacked,
}

/// <summary>
/// A main panel (a list) with a secondary one (the detail). With room they go side by side; when the width falls below the
/// threshold the secondary panel goes under the main one, so the main content never gets a horizontal scrollbar and is
/// never cut before the secondary panel gives way (adaptabilitat-i-disposicio, Disposición adaptable).
/// </summary>
public sealed class AdaptivePanels : UserControl
{
    readonly Grid _grid = new();
    readonly Control _main;
    readonly Control _secondary;
    readonly double _secondaryWidth;

    /// <param name="secondaryWidth">The width the secondary panel takes when it is beside the main one.</param>
    public AdaptivePanels(Control main, Control secondary, double secondaryWidth = 360)
    {
        _main = main;
        _secondary = secondary;
        _secondaryWidth = secondaryWidth;
        _grid.Children.Add(main);
        _grid.Children.Add(secondary);
        Content = _grid;
        Arrange(PanelArrangement.SideBySide);
    }

    /// <summary>How the panels are arranged now.</summary>
    public PanelArrangement Arrangement { get; private set; }

    /// <summary>The single rule: the arrangement for a width. Pure, so it is tested without a window.</summary>
    public static PanelArrangement For(double width) => width < WindowLimits.StackBelowWidth ? PanelArrangement.Stacked : PanelArrangement.SideBySide;

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        var wanted = For(e.NewSize.Width);
        if (wanted != Arrangement)
        {
            Arrange(wanted);
        }
    }

    void Arrange(PanelArrangement arrangement)
    {
        Arrangement = arrangement;
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();
        if (arrangement == PanelArrangement.SideBySide)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(_secondaryWidth)));
            Grid.SetRow(_main, 0);
            Grid.SetColumn(_main, 0);
            Grid.SetRow(_secondary, 0);
            Grid.SetColumn(_secondary, 1);
        }
        else
        {
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetRow(_main, 0);
            Grid.SetColumn(_main, 0);
            Grid.SetRow(_secondary, 1);
            Grid.SetColumn(_secondary, 0);
        }
    }
}
