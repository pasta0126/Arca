// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Commands;

namespace Arca.UI.Assigning;

/// <summary>
/// The one way the interface assigns a locker to a student, whether the person dragged the student over the locker, chose
/// it from a context menu or used the keyboard (ux-fonaments, D12): it calls the same use case, asks the same confirmation
/// when other capabilities warn (debt of earlier years), and says the same thing when it is done. It runs as a run-once
/// command, so dropping twice, or dragging and clicking at the same time, assigns once.
/// </summary>
public sealed class AssignLockerInteraction
{
    readonly Func<AssignLockerRequest, CancellationToken, Task<Result<AssignLockerResult>>> _assign;
    readonly IConfirmationService _confirmations;
    readonly ILocalizer _localizer;
    readonly RunOnceCommand<AssignLockerResult> _command;
    AssignmentIntent? _pending;

    /// <param name="assign">The assignment use case. Changing a student's locker uses the same shape, with the change use case.</param>
    /// <param name="successText">What is said when it is done; by default that the student now has the locker.</param>
    /// <param name="afterAssigned">Runs after a successful assignment, with what was asked, so the screens read again what changed.</param>
    public AssignLockerInteraction(
        Func<AssignLockerRequest, CancellationToken, Task<Result<AssignLockerResult>>> assign,
        IConfirmationService confirmations, ILocalizer localizer, INotificationService notifications, IErrorLog log, IDelay delay,
        Func<AssignmentRow, string>? successText = null, Func<AssignmentIntent, Task>? afterAssigned = null)
    {
        _assign = assign;
        _confirmations = confirmations;
        _localizer = localizer;
        var texts = new AssignmentResultTexts(localizer);
        var say = successText ?? texts.Assigned;
        _command = new RunOnceCommand<AssignLockerResult>(
            RunAsync, done => say(done.Assignment!), "AssignLocker", notifications, localizer, log, delay,
            afterAssigned is null ? null : () => afterAssigned(_pending!));
    }

    /// <summary>The state of the work in progress, for the indicator that goes next to whatever started it.</summary>
    public IWorkState Work => _command;

    public bool IsRunning => _command.IsRunning;

    /// <summary>Assigns the locker to the student. While one is running, another request is ignored.</summary>
    public async Task AssignAsync(AssignmentIntent intent)
    {
        if (_command.IsRunning)
        {
            return;
        }

        _pending = intent;
        await _command.RunAsync();
    }

    async Task<Result<AssignLockerResult>> RunAsync(CancellationToken ct, IProgress<OperationProgress> progress)
    {
        var intent = _pending!;
        var first = await _assign(new AssignLockerRequest(intent.StudentId, intent.LockerId), ct);
        if (!first.IsSuccess || !first.Value!.NeedsConfirmation)
        {
            return first;
        }

        var request = new ConfirmationRequest(
            _localizer.Get("Assignments.Label.ConfirmWarningsTitle"),
            _localizer.Get("Assignments.Label.ConfirmWarningsConsequence"),
            _localizer.Get("Assignments.Label.ConfirmWarningsConfirm"),
            Destructive: false,
            Details: [.. first.Value.Warnings.Select(_localizer.Message)]);
        if (!await _confirmations.ConfirmAsync(request, ct))
        {
            throw new OperationCanceledException(); // the person decided not to: it says nothing was assigned
        }

        return await _assign(new AssignLockerRequest(intent.StudentId, intent.LockerId, ConfirmWarnings: true), ct);
    }
}
