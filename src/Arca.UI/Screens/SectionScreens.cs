// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Arca.UI.Screens;

/// <summary>A screen of a section with what builds it. The definition (its title and its section) is the one of the shell's catalogue.</summary>
public sealed record ScreenEntry(string ScreenId, Func<Control> Create);

/// <summary>
/// The root of a section that holds several screens, such as Lockers (lockers and zones): a row of tabs by screen and the chosen
/// one below. Every screen is registered in one section only, and it is the shell's catalogue who says which (D7); a screen
/// that the catalogue puts in another section is refused. Each screen is built the first time it is opened and kept.
/// </summary>
public sealed class SectionScreens : UserControl
{
    readonly Dictionary<string, Control> _built = [];
    readonly ContentControl _body = new();
    readonly IReadOnlyList<ScreenEntry> _entries;

    public SectionScreens(string sectionId, IReadOnlyList<ScreenEntry> entries, ILocalizer localizer)
    {
        foreach (var entry in entries)
        {
            var screen = ShellCatalog.Screens.FirstOrDefault(s => s.Id == entry.ScreenId)
                ?? throw new ArgumentException($"The screen '{entry.ScreenId}' is not in the catalogue.", nameof(entries));
            if (screen.SectionId != sectionId)
            {
                throw new ArgumentException($"The screen '{entry.ScreenId}' belongs to the section '{screen.SectionId}', not to '{sectionId}'.", nameof(entries));
            }
        }

        _entries = entries;
        var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Avalonia.Thickness(24, 12, 24, 0) }.Themed(StackPanel.SpacingProperty, ArcaResourceKeys.SpacingMedium);
        foreach (var entry in entries)
        {
            var button = new Button { Content = localizer.Get(ShellCatalog.Screens.First(s => s.Id == entry.ScreenId).TitleKey) };
            button.Click += (_, _) => Open(entry.ScreenId);
            Tabs.Add(entry.ScreenId, button);
            tabs.Children.Add(button);
        }

        var layout = new DockPanel();
        if (entries.Count > 1)
        {
            DockPanel.SetDock(tabs, Dock.Top);
            layout.Children.Add(tabs);
        }

        layout.Children.Add(_body);
        Content = layout;
        if (entries.Count > 0)
        {
            Open(entries[0].ScreenId);
        }
    }

    /// <summary>The button of each screen, by its name, so a test can press it.</summary>
    public Dictionary<string, Button> Tabs { get; } = [];

    /// <summary>The screen shown now.</summary>
    public string? CurrentScreenId { get; private set; }

    public void Open(string screenId)
    {
        var entry = _entries.First(e => e.ScreenId == screenId);
        if (!_built.TryGetValue(screenId, out var control))
        {
            _built[screenId] = control = entry.Create();
        }

        _body.Content = control;
        CurrentScreenId = screenId;
    }
}
