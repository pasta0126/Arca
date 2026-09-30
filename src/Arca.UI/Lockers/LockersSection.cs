// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Actions;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Screens;
using Arca.UI.Search;
using Arca.UI.Shell;
using Avalonia.Controls;

namespace Arca.UI.Lockers;

/// <summary>Builds the Lockers section: the Lockers and Zones views as tabs, with Lockers first.</summary>
public static class LockersSection
{
    public static Control Create(
        LockerServices services, LockerAssignmentServices assignment, ScreenContext context, AppAction standardNew, Arca.UI.Assigning.AssignmentDialogs assign,
        UiPreferencesSession preferences, SearchNavigator navigator, ResultNotifier notifier, ScreenFilterRouter? router = null)
    {
        var localizer = context.Localizer;
        ZonesViewModel? zones = null;
        LockersViewModel? lockers = null;
        SectionScreens? section = null;
        lockers = new LockersViewModel(services, context, standardNew, () =>
        {
            section!.Open("Zones");
            return zones!.NewZoneAsync();
        }, assign, () =>
        {
            section!.Open("LockerMap");
            return Task.CompletedTask;
        });
        zones = new ZonesViewModel(services, context, () => lockers.LoadAsync());
        var map = new LockersMapModel(lockers, assignment, notifier, context.Confirmations, localizer, context.Notifications, context.Log, context.Delay);
        section = new SectionScreens(
            ShellCatalog.Lockers,
            [
                new("LockerMap", () => LockersView.CreateMap(lockers, map, localizer, preferences)),
                new("Lockers", () => LockersView.Create(lockers, localizer)),
                new("Zones", () => ZonesView.Create(zones, localizer)),
            ],
            localizer);

        // A locker chosen in the search opens the map with it outlined and its detail open, also when the section is built by that very choice.
        navigator.Requested += (_, target) => Reveal(target);
        if (router is not null)
        {
            // A card of the start screen opens the map, or the screen it names, with a filter on.
            void Take()
            {
                if (router.Take(ShellCatalog.Lockers) is { } request)
                {
                    section.Open(request.Screen ?? "LockerMap");
                    lockers.ApplyRequest(request.Filters);
                }
            }

            router.Requested += (_, request) =>
            {
                if (request.Section == ShellCatalog.Lockers)
                {
                    Take();
                }
            };
            Take();
        }

        _ = LoadAsync();
        return section;

        async Task LoadAsync()
        {
            await lockers.LoadAsync();
            if (navigator.TakePending() is { } pending)
            {
                Reveal(pending);
            }
        }

        void Reveal(SearchTarget target)
        {
            if (target.Kind == SearchTargetKind.Locker)
            {
                section.Open("LockerMap");
                lockers.Reveal(target.Id);
            }
        }
    }
}
