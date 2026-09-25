// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Lockers;

/// <summary>
/// The types of history event of a locker (taquilles-i-zones, D7). They are stable codes, never translated text; the text
/// is composed from the key History.&lt;type&gt;. Later changes add their own types to the same history.
/// </summary>
public static class LockerEventTypes
{
    public const string Created = "Locker.Created";
    public const string NumberChanged = "Locker.NumberChanged";
    public const string ZoneChanged = "Locker.ZoneChanged";
    public const string Reserved = "Locker.Reserved";
    public const string ReservationRemoved = "Locker.ReservationRemoved";
    public const string OutOfService = "Locker.OutOfService";
    public const string ServiceTypeChanged = "Locker.ServiceTypeChanged";
    public const string ServiceRestored = "Locker.ServiceRestored";
    public const string Retired = "Locker.Retired";

    /// <summary>Every type declared here, for the tests that check each one has its text.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Created, NumberChanged, ZoneChanged, Reserved, ReservationRemoved, OutOfService, ServiceTypeChanged, ServiceRestored, Retired];
}
