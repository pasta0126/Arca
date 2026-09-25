// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Assignments.ChangeStudentLocker;

/// <param name="StudentId">The student that holds a locker now.</param>
/// <param name="NewLockerId">The locker they move to.</param>
/// <param name="ConfirmWarnings">True once the person has confirmed the warnings raised about the new locker.</param>
/// <param name="Operation">The reason and extra data that go to the hooks, for example what happened to the key.</param>
public sealed record ChangeStudentLockerRequest(Guid StudentId, Guid NewLockerId, bool ConfirmWarnings = false, OperationContext? Operation = null);
