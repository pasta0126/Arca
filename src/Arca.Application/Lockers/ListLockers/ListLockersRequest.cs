// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers.ListLockers;

public sealed record ListLockersRequest(LockerFilter? Filter = null);
