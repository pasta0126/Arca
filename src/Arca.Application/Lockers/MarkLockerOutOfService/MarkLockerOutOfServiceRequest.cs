// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Lockers;

namespace Arca.Application.Lockers.MarkLockerOutOfService;

/// <param name="LockerId">The locker.</param>
/// <param name="Kind">Broken or in maintenance.</param>
/// <param name="Decision">What to do with the student when the locker is occupied; null until the person has decided.</param>
public sealed record MarkLockerOutOfServiceRequest(Guid LockerId, OutOfServiceKind Kind, OutOfServiceDecision? Decision = null);
