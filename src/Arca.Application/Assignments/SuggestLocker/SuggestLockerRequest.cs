// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Assignments.SuggestLocker;

/// <param name="ZoneId">The zone chosen; when null, the first active zone in order.</param>
public sealed record SuggestLockerRequest(Guid? ZoneId = null);
