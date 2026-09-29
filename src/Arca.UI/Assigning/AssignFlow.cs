// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;

namespace Arca.UI.Assigning;

/// <summary>
/// The two steps every way of assigning a locker shares (dragging, from the student, from the locker): try the assignment; if it
/// raised warnings, nothing was done, so show them and only repeat it, confirmed, if the person explicitly agrees. One place, so the
/// three ways can never differ.
/// </summary>
public static class AssignFlow
{
    /// <param name="debtDetails">The debt of the student by concept and year, in words, added to the warnings so the person sees what they are confirming.</param>
    /// <param name="throwIfDeclined">Declining the warnings throws a cancellation, which the run-once command reports as cancelled. Otherwise the answer is null.</param>
    /// <returns>The result, or null when the person declined the warnings and nothing was assigned.</returns>
    public static async Task<Result<AssignLockerResult>?> RunAsync(
        Func<AssignLockerRequest, CancellationToken, Task<Result<AssignLockerResult>>> assign, AssignmentIntent intent,
        IConfirmationService confirmations, ILocalizer localizer, CancellationToken ct, bool throwIfDeclined = false,
        Func<Guid, CancellationToken, Task<IReadOnlyList<string>>>? debtDetails = null)
    {
        var first = await assign(new AssignLockerRequest(intent.StudentId, intent.LockerId), ct);
        if (!first.IsSuccess || !first.Value!.NeedsConfirmation)
        {
            return first;
        }

        var request = new ConfirmationRequest(
            localizer.Get("Assignments.Label.ConfirmWarningsTitle"),
            localizer.Get("Assignments.Label.ConfirmWarningsConsequence"),
            localizer.Get("Assignments.Label.ConfirmWarningsConfirm"),
            Destructive: false,
            Details: [.. first.Value.Warnings.Select(localizer.Message), .. debtDetails is null ? [] : await debtDetails(intent.StudentId, ct)]);
        if (!await confirmations.ConfirmAsync(request, ct))
        {
            return throwIfDeclined ? throw new OperationCanceledException() : null; // the person decided not to: nothing was assigned
        }

        return await assign(new AssignLockerRequest(intent.StudentId, intent.LockerId, ConfirmWarnings: true), ct);
    }
}
