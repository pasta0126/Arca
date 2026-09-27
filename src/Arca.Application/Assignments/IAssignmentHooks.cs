// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Assignments;
using Arca.Domain.Students;

namespace Arca.Application.Assignments;

/// <summary>An assignment that was opened or closed, with the context of the operation that did it.</summary>
public sealed record AssignmentHookContext(Assignment Assignment, OperationContext Operation);

/// <summary>
/// Called inside the same transaction that opens an assignment (assign, change, reassign), so nothing is left half done
/// (alumnes-i-assignacions, D9). The charges of pagaments will hang here. If it throws, everything is undone.
/// </summary>
public interface IAssignmentOpenedHandler
{
    Task HandleAsync(AssignmentHookContext context, CancellationToken ct);
}

/// <summary>
/// Called inside the same transaction that closes an assignment (release, change, retirement, breakdown). The state of the
/// key of claus will hang here. If it throws, everything is undone.
/// </summary>
public interface IAssignmentClosedHandler
{
    Task HandleAsync(AssignmentHookContext context, CancellationToken ct);
}

/// <summary>Called inside the transaction that retires or reactivates a student. The deposit of pagaments will hang here.</summary>
public interface IStudentLifecycleHandler
{
    Task OnRetiredAsync(Student student, OperationContext operation, CancellationToken ct);

    Task OnReactivatedAsync(Student student, OperationContext operation, CancellationToken ct);
}
