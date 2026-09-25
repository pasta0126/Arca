// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers.ReserveLocker;

public sealed record ReserveLockerRequest(Guid LockerId, string? Note = null);
