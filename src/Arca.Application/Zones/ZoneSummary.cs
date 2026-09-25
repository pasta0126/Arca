// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Zones;

namespace Arca.Application.Zones;

/// <summary>A zone as the lists and results show it, with how many lockers that are not retired it has.</summary>
public sealed record ZoneSummary(Guid Id, string Name, bool IsActive, int ActiveLockers)
{
    public static ZoneSummary Of(Zone zone, int activeLockers) => new(zone.Id, zone.Name, zone.IsActive, activeLockers);
}
