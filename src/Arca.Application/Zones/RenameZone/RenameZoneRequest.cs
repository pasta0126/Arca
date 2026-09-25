// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Zones.RenameZone;

public sealed record RenameZoneRequest(Guid ZoneId, string? Name);
