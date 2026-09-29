// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.UI.Common;
using Material.Icons;
using Material.Icons.Avalonia;

namespace Arca.UI.Shell;

/// <summary>
/// The icon of each section, taken from the Material Design Icons set. The state of something is never shown by an icon
/// alone: the section always has its name too, in the sidebar or in its tooltip when the sidebar is folded.
/// </summary>
public static class SectionIcons
{
    static readonly Dictionary<string, MaterialIconKind> _kinds = new()
    {
        [ShellCatalog.Home] = MaterialIconKind.Home,
        [ShellCatalog.Lockers] = MaterialIconKind.LockerMultiple,
        [ShellCatalog.Students] = MaterialIconKind.AccountGroupOutline,
        [ShellCatalog.Payments] = MaterialIconKind.CashMultiple,
        [ShellCatalog.KeysAndIncidents] = MaterialIconKind.KeyVariant,
        [ShellCatalog.Reports] = MaterialIconKind.ChartBar,
        [ShellCatalog.Course] = MaterialIconKind.CalendarMonthOutline,
        [ShellCatalog.Settings] = MaterialIconKind.Cog,
    };

    /// <summary>The icon of a section, or a plain square if the section has none.</summary>
    public static MaterialIconKind KindOf(string sectionId) => _kinds.GetValueOrDefault(sectionId, MaterialIconKind.Square);

    /// <summary>The icon as a control, coloured by the theme.</summary>
    public static MaterialIcon Create(string sectionId) => ThemedIcon.Create(KindOf(sectionId));
}
