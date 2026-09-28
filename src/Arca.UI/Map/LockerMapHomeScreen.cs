// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.UI.Search;
using Arca.UI.Shell;
using Avalonia.Controls;

namespace Arca.UI.Map;

/// <summary>
/// The default start screen (ui-shell, D2): the map of lockers by zone. It is a proposal that stays open until the caretakers
/// validate it; another <see cref="IHomeScreen"/> replaces it without touching anything else. It loads the map when it is built
/// and reacts to a locker chosen in the search, whether that happened before the screen existed or while it is showing.
/// </summary>
public sealed class LockerMapHomeScreen(LockerMapViewModel model, SearchNavigator navigator, ILocalizer localizer) : IHomeScreen
{
    public Control Create()
    {
        var view = new LockerMapView(model, localizer);
        navigator.Requested += (_, target) => Reveal(target);
        _ = LoadAsync();
        return new ScreenView(localizer.Get("Shell.Screen.LockerMap"), [], view);
    }

    async Task LoadAsync()
    {
        await model.LoadAsync();
        if (navigator.TakePending() is { } pending)
        {
            Reveal(pending); // the person chose a locker in the search before this screen was built
        }
    }

    void Reveal(SearchTarget target)
    {
        if (target.Kind == SearchTargetKind.Locker)
        {
            model.Reveal(target.Id);
        }
    }
}
