// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Common;
using Arca.UI.Search;
using Arca.UI.Shell;
using Arca.UI.Theme;
using Avalonia.Controls;

namespace Arca.UI.Map;

/// <summary>
/// The default start screen (ui-shell, D2): the map of lockers by zone with, beside it, the detail of the chosen locker and the
/// panel of students without a locker. It is a proposal that stays open until the caretakers validate it; another
/// <see cref="IHomeScreen"/> replaces it without touching anything else. It loads when it is built and reacts to a locker chosen
/// in the search, whether that happened before the screen existed or while it is showing.
/// </summary>
public sealed class LockerMapHomeScreen(LockerHomeModel home, SearchNavigator navigator, ILocalizer localizer) : IHomeScreen
{
    public Control Create()
    {
        var map = new LockerMapView(home.Map, localizer, home.Drop);
        var side = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        var detail = new LockerDetailView(home.Detail, localizer);
        var students = new StudentsPanelView(home.Students, home.Drop, home.AssignToSelected, localizer);
        Grid.SetRow(students, 1);
        side.Children.Add(detail);
        side.Children.Add(students);

        navigator.Requested += (_, target) => Reveal(target);
        _ = LoadAsync();
        return new ScreenView(localizer.Get("Shell.Screen.LockerMap"), [], map, side);
    }

    async Task LoadAsync()
    {
        await home.LoadAsync();
        if (navigator.TakePending() is { } pending)
        {
            Reveal(pending); // the person chose a locker in the search before this screen was built
        }
    }

    void Reveal(SearchTarget target)
    {
        if (target.Kind == SearchTargetKind.Locker)
        {
            home.Map.Reveal(target.Id);
        }
    }
}
