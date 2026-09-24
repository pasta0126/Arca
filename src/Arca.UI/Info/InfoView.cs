// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Arca.UI.Info;

public sealed class InfoView : UserControl
{
    public InfoView(InfoViewModel model)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 16,
            RowSpacing = 8,
        };
        AddRow(grid, 0, model.ApplicationVersionLabel, model.ApplicationVersion);
        AddRow(grid, 1, model.SchemaVersionLabel, model.SchemaVersion);

        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = model.Title, FontSize = 20, FontWeight = FontWeight.SemiBold },
                grid,
            },
        };
    }

    static void AddRow(Grid grid, int row, string label, string value)
    {
        var name = new TextBlock { Text = label, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
        var text = new TextBlock { Text = value, IsHitTestVisible = true };
        Grid.SetRow(name, row);
        Grid.SetRow(text, row);
        Grid.SetColumn(text, 1);
        grid.Children.Add(name);
        grid.Children.Add(text);
    }
}
