// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Lockers.CreateLockerRange;

namespace Arca.Application.Lockers;

/// <summary>
/// The confirmations that taquilles-i-zones asks for, prepared as localized data with their consequence
/// (docs/convenciones.md, section 5): the screen shows them through the confirmation service and only calls the use
/// case if the person explicitly confirms.
/// </summary>
public sealed class LockerConfirmations(ILocalizer localizer)
{
    /// <summary>Retiring a locker cannot be undone. The history is kept and another locker with the number can be added.</summary>
    public ConfirmationRequest ForRetire(LockerRow locker) => new(
        localizer.Get("Lockers.Label.RetireTitle", locker.Number),
        localizer.Get("Lockers.Label.RetireConsequence", locker.Number, locker.ZoneName),
        localizer.Get("Lockers.Label.RetireConfirm"),
        Destructive: true);

    /// <summary>Creating a range says how many lockers will be created and where.</summary>
    public ConfirmationRequest ForRange(CreateLockerRangePlan plan) => new(
        localizer.Get("Lockers.Label.RangeTitle"),
        localizer.Get("Lockers.Label.RangeConsequence", plan.ToCreate.Count, plan.ZoneName),
        localizer.Get("Lockers.Label.RangeConfirm", plan.ToCreate.Count),
        Destructive: false,
        Details: [localizer.Get("Lockers.Label.RangeNumbers", plan.First, plan.Last)]);
}
