// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

namespace Arca.Application.Assignments.ReleaseStudentLocker;

/// <param name="StudentId">The student whose locker is released.</param>
/// <param name="Operation">The optional reason and extra data that go to the hooks.</param>
public sealed record ReleaseStudentLockerRequest(Guid StudentId, OperationContext? Operation = null);
