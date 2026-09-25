// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Assignments.ReserveLockerForStudent;

/// <param name="LockerId">A free locker.</param>
/// <param name="StudentId">The student the locker is reserved for.</param>
/// <param name="Note">An optional free note.</param>
public sealed record ReserveLockerForStudentRequest(Guid LockerId, Guid StudentId, string? Note = null);
