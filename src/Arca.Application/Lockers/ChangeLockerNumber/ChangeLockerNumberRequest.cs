// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Lockers.ChangeLockerNumber;

public sealed record ChangeLockerNumberRequest(Guid LockerId, int Number);
