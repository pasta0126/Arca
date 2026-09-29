// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Screens;
using Arca.UI.Shell;
using Avalonia.Controls;

namespace Arca.UI.Lockers;

/// <summary>Builds the Lockers section: the Lockers and Zones views as tabs, with Lockers first.</summary>
public static class LockersSection
{
    public static Control Create(LockerServices services, ScreenContext context, AppAction standardNew, Arca.UI.Assigning.AssignmentDialogs assign)
    {
        var localizer = context.Localizer;
        ZonesViewModel? zones = null;
        LockersViewModel? lockers = null;
        SectionScreens? section = null;
        lockers = new LockersViewModel(services, context, standardNew, () =>
        {
            section!.Open("Zones");
            return zones!.NewZoneAsync();
        }, assign);
        zones = new ZonesViewModel(services, context, () => lockers.LoadAsync());
        section = new SectionScreens(
            ShellCatalog.Lockers,
            [
                new("Lockers", () => LockersView.Create(lockers, localizer)),
                new("Zones", () => ZonesView.Create(zones, localizer)),
            ],
            localizer);
        return section;
    }
}
