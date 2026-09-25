// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Students;

/// <summary>
/// The outcome of a change that may need the person to confirm new catalogue values first: either it was done and the
/// student is returned, or nothing was changed and the values to confirm are listed (alumnes-i-assignacions, D6).
/// </summary>
/// <param name="Student">The student as they are now, or null when a confirmation is needed.</param>
/// <param name="NewValues">The levels and groups that do not exist yet and would be created if the person confirms.</param>
public sealed record StudentChangeResult(StudentDetail? Student, IReadOnlyList<NewCatalogValue> NewValues)
{
    public bool NeedsConfirmation => Student is null;

    public static Result<StudentChangeResult> Done(StudentDetail student, params Notice[] notices) =>
        Result<StudentChangeResult>.Success(new StudentChangeResult(student, []), notices);

    public static Result<StudentChangeResult> Confirm(IReadOnlyList<NewCatalogValue> values) =>
        Result<StudentChangeResult>.Success(new StudentChangeResult(null, values));
}
