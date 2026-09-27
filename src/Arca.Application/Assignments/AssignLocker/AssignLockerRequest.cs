// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Assignments.AssignLocker;

/// <param name="StudentId">The student that will hold the locker.</param>
/// <param name="LockerId">The locker.</param>
/// <param name="ConfirmWarnings">True once the person has seen and explicitly confirmed the warnings that other capabilities raised.</param>
/// <param name="Operation">The reason and extra data that go to the hooks; none by default.</param>
public sealed record AssignLockerRequest(Guid StudentId, Guid LockerId, bool ConfirmWarnings = false, OperationContext? Operation = null);
