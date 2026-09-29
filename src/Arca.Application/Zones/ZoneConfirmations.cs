// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;

namespace Arca.Application.Zones;

/// <summary>The confirmation of deleting a zone, prepared as localized data: the screen shows it and only deletes if the person confirms.</summary>
public sealed class ZoneConfirmations(ILocalizer localizer)
{
    /// <summary>Deleting a zone cannot be undone; only a zone that never had a locker can be deleted.</summary>
    public ConfirmationRequest ForDelete(string zoneName) => new(
        localizer.Get("Zones.Label.DeleteTitle", zoneName),
        localizer.Get("Zones.Label.DeleteConsequence", zoneName),
        localizer.Get("Zones.Label.DeleteConfirm"), Destructive: true);
}
