// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Zones.ListZones;

/// <param name="IncludeInactive">Also list the deactivated zones. By default only the active ones.</param>
public sealed record ListZonesRequest(bool IncludeInactive = false);
