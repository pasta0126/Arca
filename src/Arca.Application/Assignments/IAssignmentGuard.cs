// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Lockers;
using Arca.Domain.SchoolYears;
using Arca.Domain.Students;

namespace Arca.Application.Assignments;

/// <summary>Whether a finding only warns, and needs the person to confirm, or blocks the assignment.</summary>
public enum AssignmentFindingKind
{
    /// <summary>The assignment can go ahead once the person has explicitly confirmed it.</summary>
    Warning,

    /// <summary>The assignment is refused and the person cannot confirm it.</summary>
    Blocker,
}

/// <summary>
/// One finding of a guard about a proposed assignment. The code is stable: as a warning its text is
/// Capacity.Warning.Name, and as a blocker it becomes a business error whose text is Capacity.Error.Name.
/// </summary>
public sealed record AssignmentFinding(AssignmentFindingKind Kind, string Code, IReadOnlyList<object>? Args = null);

/// <summary>The assignment a guard is asked about.</summary>
public sealed record ProposedAssignment(Student Student, Locker Locker, AcademicYear Year);

/// <summary>
/// A check that other capabilities add to an assignment (alumnes-i-assignacions, D8): pagaments will warn about debt of
/// earlier years and claus will block a locker without an available key. This change defines it and calls it, with no
/// implementations.
/// </summary>
public interface IAssignmentGuard
{
    Task<IReadOnlyList<AssignmentFinding>> CheckAsync(ProposedAssignment proposal, CancellationToken ct);
}
