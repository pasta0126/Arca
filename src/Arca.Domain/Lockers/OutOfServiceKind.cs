// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Domain.Lockers;

/// <summary>The two kinds of "out of service" (taquilles-i-zones). They exclude each other: a locker is in one or none.</summary>
public enum OutOfServiceKind
{
    Broken,
    Maintenance,
}
