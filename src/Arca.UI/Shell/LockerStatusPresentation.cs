// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Application.Search;
using Arca.UI.Theme;
using Material.Icons;

namespace Arca.UI.Shell;

/// <summary>
/// How the status of a locker is shown everywhere (ui-shell, Estado visible): a colour from the theme, an icon and a word, so
/// the status is never told by colour alone. The map, the detail and the search results use the same one.
/// </summary>
public static class LockerStatusPresentation
{
    /// <summary>The word for the status, in the user's language.</summary>
    public static string Text(LockerStatusView status, ILocalizer localizer) => status switch
    {
        LockerStatusView.Free => localizer.Get("Shell.Search.Status.Free"),
        LockerStatusView.Occupied => localizer.Get("Shell.Search.Status.Occupied"),
        LockerStatusView.Reserved => localizer.Get("Shell.Search.Status.Reserved"),
        LockerStatusView.Broken => localizer.Get("Shell.Search.Status.Broken"),
        LockerStatusView.Maintenance => localizer.Get("Shell.Search.Status.Maintenance"),
        _ => localizer.Get("Shell.Search.Status.Retired"),
    };

    /// <summary>The theme resource of its colour.</summary>
    public static string BrushKey(LockerStatusView status) => status switch
    {
        LockerStatusView.Free => ArcaResourceKeys.StatusFree,
        LockerStatusView.Occupied => ArcaResourceKeys.StatusOccupied,
        LockerStatusView.Reserved => ArcaResourceKeys.StatusReserved,
        LockerStatusView.Broken => ArcaResourceKeys.StatusBroken,
        LockerStatusView.Maintenance => ArcaResourceKeys.StatusMaintenance,
        _ => ArcaResourceKeys.Surface,
    };

    /// <summary>Its icon.</summary>
    public static MaterialIconKind Icon(LockerStatusView status) => status switch
    {
        LockerStatusView.Free => MaterialIconKind.LockOpenVariantOutline,
        LockerStatusView.Occupied => MaterialIconKind.Lock,
        LockerStatusView.Reserved => MaterialIconKind.Bookmark,
        LockerStatusView.Broken => MaterialIconKind.AlertCircleOutline,
        LockerStatusView.Maintenance => MaterialIconKind.Wrench,
        _ => MaterialIconKind.Close,
    };

    /// <summary>The mark of a locker whose student owes something.</summary>
    public const MaterialIconKind DebtIcon = MaterialIconKind.CurrencyEur;
}
