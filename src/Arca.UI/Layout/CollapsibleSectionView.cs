// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace Arca.UI.Layout;

/// <summary>
/// The look of a collapsible section: a header with the title and, when folded, its summary, and the content below. It is
/// an <see cref="Expander"/>, so the mouse and the keyboard (Tab to reach it, Space or Enter to fold it) work as standard.
/// </summary>
public sealed class CollapsibleSectionView : UserControl
{
    public CollapsibleSectionView(CollapsibleSectionViewModel model, Control content)
    {
        DataContext = model;
        var title = new TextBlock { Text = model.Title, FontWeight = FontWeight.SemiBold }
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.Text)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeBody);
        var summary = new TextBlock()
            .Themed(TextBlock.ForegroundProperty, ArcaResourceKeys.TextSecondary)
            .Themed(TextBlock.FontSizeProperty, ArcaResourceKeys.FontSizeSmall);
        summary.Bind(TextBlock.TextProperty, new Binding(nameof(CollapsibleSectionViewModel.Summary)));
        summary.Bind(IsVisibleProperty, new Binding(nameof(CollapsibleSectionViewModel.Summary)) { Converter = Avalonia.Data.Converters.StringConverters.IsNotNullOrEmpty });

        var header = new StackPanel().Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingSmall);
        header.Children.Add(title);
        header.Children.Add(summary);

        Section = new Expander { Header = header, Content = content, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        Section.Bind(Expander.IsExpandedProperty, new Binding(nameof(CollapsibleSectionViewModel.IsExpanded)) { Mode = BindingMode.TwoWay });
        Content = Section;
    }

    /// <summary>The expander, so a screen or a test can reach the control that takes the focus.</summary>
    public Expander Section { get; }
}
