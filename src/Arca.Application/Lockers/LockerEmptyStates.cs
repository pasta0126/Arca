// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;

namespace Arca.Application.Lockers;

/// <summary>Turns an empty state into its guidance: what happened and how to go on (docs: "Estados vacíos y de carga con guía").</summary>
public static class LockerEmptyStates
{
    public static EmptyStateGuide? Describe(LockerEmptyState state, ILocalizer localizer) => state switch
    {
        LockerEmptyState.NoZones => new EmptyStateGuide(localizer.Get("Lockers.Empty.NoZones"), [SuggestedAction.CreateZone]),
        LockerEmptyState.NoLockers => new EmptyStateGuide(
            localizer.Get("Lockers.Empty.NoLockers"), [SuggestedAction.CreateRange, SuggestedAction.AddLocker]),
        LockerEmptyState.NoResults => new EmptyStateGuide(localizer.Get("Lockers.Empty.NoResults"), [SuggestedAction.ClearFilters]),
        _ => null,
    };

    /// <summary>The label of a suggested action, for its button.</summary>
    public static string Label(SuggestedAction action, ILocalizer localizer) => action switch
    {
        SuggestedAction.CreateZone => localizer.Get("Lockers.Label.CreateZone"),
        SuggestedAction.CreateRange => localizer.Get("Lockers.Label.CreateRange"),
        SuggestedAction.AddLocker => localizer.Get("Lockers.Label.AddLocker"),
        SuggestedAction.ClearFilters => localizer.Get("Lockers.Label.ClearFilters"),
        _ => string.Empty,
    };
}
