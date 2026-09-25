// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers.AddLocker;

/// <param name="Number">The number a person sees.</param>
/// <param name="ZoneId">An active zone.</param>
/// <param name="Note">An optional free note of up to 500 characters.</param>
public sealed record AddLockerRequest(int Number, Guid ZoneId, string? Note = null);
