// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.UI.Common;

namespace Arca.UI.Assigning;

/// <summary>Whether a locker can receive the student being dragged.</summary>
public enum DropTargetState
{
    Valid,
    Invalid,
}

/// <summary>What a locker shows while a student is dragged over it: valid, or not valid and why.</summary>
/// <param name="State">Whether it can be dropped here.</param>
/// <param name="Reason">Why not, in the user's language, when it is not valid.</param>
/// <param name="NeedsConfirmation">A valid drop that will ask for confirmation first (a warning, such as earlier debt).</param>
public sealed record DropTargetFeedback(DropTargetState State, string? Reason, bool NeedsConfirmation);

/// <summary>
/// The behaviour of dragging a student onto a locker, without any window (ux-fonaments, D2 and D12). While dragging, each
/// locker asks whether it is a valid destination, with the same rules as the assignment itself and writing nothing. Dropping
/// on a valid one runs the assignment through <see cref="AssignLockerInteraction"/>, like the menu and the keyboard do.
/// Escape or dropping anywhere else changes nothing, and dropping twice assigns once.
/// </summary>
public sealed class AssignmentDropViewModel(
    Func<Guid, Guid, CancellationToken, Task<Result<AssignmentTargetCheck>>> check, AssignLockerInteraction interaction, ILocalizer localizer)
    : ObservableObject
{
    readonly Dictionary<Guid, DropTargetFeedback> _feedback = [];
    Guid? _student;
    bool _isDropping;

    /// <summary>Whether a student is being dragged now.</summary>
    public bool IsDragging => _student is not null;

    /// <summary>A student starts being dragged.</summary>
    public void BeginDrag(Guid studentId)
    {
        _student = studentId;
        _feedback.Clear();
        Raise(nameof(IsDragging));
    }

    /// <summary>The drag ended without dropping on a valid locker (Escape, dropped outside): nothing changes.</summary>
    public void End()
    {
        _student = null;
        _feedback.Clear();
        Raise(nameof(IsDragging));
    }

    /// <summary>What a locker answered already, or null if it was not asked yet. Cheap, so a drag-over can use it at once.</summary>
    public DropTargetFeedback? Known(Guid lockerId) => _feedback.GetValueOrDefault(lockerId);

    /// <summary>Asks whether the locker can receive the student being dragged. Null when nothing is being dragged.</summary>
    public async Task<DropTargetFeedback?> PreviewAsync(Guid lockerId, CancellationToken ct = default)
    {
        if (_student is not { } student)
        {
            return null;
        }

        if (_feedback.TryGetValue(lockerId, out var known))
        {
            return known;
        }

        var answer = await check(student, lockerId, ct);
        var feedback = !answer.IsSuccess ? new DropTargetFeedback(DropTargetState.Invalid, localizer.Message(answer.Error!), false)
            : answer.Value!.IsValid ? new DropTargetFeedback(DropTargetState.Valid, null, answer.Value.Warnings.Count > 0)
            : new DropTargetFeedback(DropTargetState.Invalid, localizer.Message(answer.Value.Blocker!), false);
        if (_student == student)
        {
            _feedback[lockerId] = feedback;
        }

        return feedback;
    }

    /// <summary>The student was dropped on a locker. Assigns only if it is a valid destination.</summary>
    /// <returns>True if the assignment was started.</returns>
    public async Task<bool> DropAsync(Guid lockerId)
    {
        if (_student is not { } student || _isDropping)
        {
            return false;
        }

        var feedback = await PreviewAsync(lockerId);
        if (feedback is not { State: DropTargetState.Valid })
        {
            End();
            return false;
        }

        _isDropping = true;
        try
        {
            End();
            await interaction.AssignAsync(new AssignmentIntent(student, lockerId));
            return true;
        }
        finally
        {
            _isDropping = false;
        }
    }
}
