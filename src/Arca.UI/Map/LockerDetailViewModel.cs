// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Search;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Assigning;
using Arca.UI.Commands;
using Arca.UI.Common;
using Arca.UI.Notifications;

namespace Arca.UI.Map;

/// <summary>
/// The operations of a locker that change data, as the composition offers them: each takes the identity it acts on (the student
/// for releasing, the locker for the rest) and answers with the sentence that says what was done. The screen runs them only
/// through run-once commands.
/// </summary>
public sealed record LockerOperations(
    Func<Guid, CancellationToken, Task<Result<string>>> Release,
    Func<Guid, CancellationToken, Task<Result<string>>> Reserve,
    Func<Guid, CancellationToken, Task<Result<string>>> RemoveReservation,
    Func<Guid, CancellationToken, Task<Result<string>>> MarkBroken,
    Func<Guid, CancellationToken, Task<Result<string>>> Restore);

/// <summary>What the detail of a locker needs from the rest of the screen.</summary>
public sealed record LockerDetailContext(
    Func<Guid, CancellationToken, Task<Result<LockerDetail?>>> Load, LockerOperations Operations, AssignLockerInteraction Assign,
    IConfirmationService Confirmations, ILocalizer Localizer, INotificationService Notifications, IErrorLog Log, IDelay Delay,
    StudentsWithoutLockerViewModel Students, LockerMapViewModel Map, Func<IEnumerable<Guid>, Task> AfterWrite);

/// <summary>
/// The panel of the locker chosen on the map (ui-shell, Detalle de la taquilla seleccionada): its status, its student with level,
/// group and payment state, and the actions that apply to it, which are the same objects as any button, menu or shortcut. An
/// action that does not apply to the locker in its state is shown disabled with the reason.
/// </summary>
public sealed class LockerDetailViewModel : ObservableObject
{
    readonly LockerDetailContext _context;
    readonly Operation _release, _reserve, _removeReservation, _markBroken, _restore;
    LockerDetail? _detail;
    IReadOnlyList<AppAction> _actions = [];
    int _request;

    public LockerDetailViewModel(LockerDetailContext context)
    {
        _context = context;
        _release = new Operation(this, context.Operations.Release, "ReleaseLocker");
        _reserve = new Operation(this, context.Operations.Reserve, "ReserveLocker");
        _removeReservation = new Operation(this, context.Operations.RemoveReservation, "RemoveReservation");
        _markBroken = new Operation(this, context.Operations.MarkBroken, "MarkLockerBroken");
        _restore = new Operation(this, context.Operations.Restore, "RestoreLocker");
        context.Students.PropertyChanged += (_, _) => RefreshAvailability();
    }

    /// <summary>The detail of the locker chosen, or null when none is.</summary>
    public LockerDetail? Detail
    {
        get => _detail;
        private set => Set(ref _detail, value);
    }

    /// <summary>The actions of the locker in the order they are offered.</summary>
    public IReadOnlyList<AppAction> Actions
    {
        get => _actions;
        private set => Set(ref _actions, value);
    }

    /// <summary>Shows the locker, or nothing with null. Called when one is chosen and again after every change.</summary>
    public async Task ShowAsync(Guid? lockerId, CancellationToken ct = default)
    {
        var mine = ++_request; // only the answer to the latest request is shown: an older one that arrives late is dropped
        if (lockerId is not { } id)
        {
            Detail = null;
            Actions = [];
            return;
        }

        try
        {
            var result = await _context.Load(id, ct);
            if (mine != _request)
            {
                return;
            }

            if (!result.IsSuccess)
            {
                new ResultNotifier(_context.Notifications, _context.Localizer, _context.Log).Error(result.Error!);
                return;
            }

            Detail = result.Value;
            Actions = result.Value is null ? [] : Build(result.Value);
        }
        catch (OperationCanceledException)
        {
            // Another locker was chosen meanwhile.
        }
        catch (Exception e)
        {
            new ResultNotifier(_context.Notifications, _context.Localizer, _context.Log).Unexpected(e, "LockerDetail");
        }
    }

    void RefreshAvailability()
    {
        foreach (var action in _actions)
        {
            action.Refresh();
        }
    }

    List<AppAction> Build(LockerDetail detail)
    {
        var text = _context.Localizer;
        var students = _context.Students;
        var free = detail.Status == LockerStatusView.Free;
        var occupied = detail.Status == LockerStatusView.Occupied;
        var reserved = detail.Status == LockerStatusView.Reserved;
        var outOfService = detail.Status is LockerStatusView.Broken or LockerStatusView.Maintenance;
        var actions = new List<AppAction>();

        AppAction Make(string id, string labelKey, Action run, Func<Availability> availability)
        {
            var action = new AppAction(id, text.Get(labelKey));
            action.Attach(run, availability);
            actions.Add(action);
            return action;
        }

        Availability Reason(bool applies, string reasonKey) => applies ? Availability.Available : Availability.Unavailable(text.Get(reasonKey));

        Make("Assign", "Shell.Action.Assign", () => _ = AssignAsync(detail), () =>
            students.NoActiveYear ? Availability.Unavailable(text.Get("Shell.Reason.NoYear"))
            : occupied ? Availability.Unavailable(text.Get("Shell.Reason.Occupied"))
            : reserved ? Availability.Unavailable(text.Get("Shell.Reason.Reserved"))
            : outOfService ? Availability.Unavailable(text.Get("Shell.Reason.OutOfService"))
            : students.SelectedStudentId is null ? Availability.Unavailable(text.Get("Shell.Reason.PickStudent"))
            : Availability.Available);
        Make("Change", "Shell.Action.Change", () => _context.Map.BeginPick(detail.StudentId!.Value, detail.StudentName ?? string.Empty), () => Reason(occupied, "Shell.Reason.NoStudent"));
        Make("Release", "Shell.Action.Release", () => _ = ReleaseAsync(detail), () => Reason(occupied, "Shell.Reason.NoStudent"));
        Make("Reserve", "Shell.Action.Reserve", () => _ = _reserve.RunAsync(detail.LockerId, detail.LockerId), () =>
            free ? Availability.Available : Availability.Unavailable(text.Get(occupied ? "Shell.Reason.Occupied" : reserved ? "Shell.Reason.Reserved" : "Shell.Reason.OutOfService")));
        Make("RemoveReservation", "Shell.Action.RemoveReservation", () => _ = _removeReservation.RunAsync(detail.LockerId, detail.LockerId), () => Reason(reserved, "Shell.Reason.NotReserved"));
        Make("MarkBroken", "Shell.Action.MarkBroken", () => _ = _markBroken.RunAsync(detail.LockerId, detail.LockerId), () =>
            free ? Availability.Available : Availability.Unavailable(text.Get(occupied ? "Shell.Reason.ReleaseFirst" : reserved ? "Shell.Reason.Reserved" : "Shell.Reason.OutOfService")));
        Make("Restore", "Shell.Action.Restore", () => _ = _restore.RunAsync(detail.LockerId, detail.LockerId), () => Reason(outOfService, "Shell.Reason.InService"));
        return actions;
    }

    Task AssignAsync(LockerDetail detail) =>
        _context.Students.SelectedStudentId is { } student
            ? _context.Assign.AssignAsync(new AssignmentIntent(student, detail.LockerId))
            : Task.CompletedTask;

    async Task ReleaseAsync(LockerDetail detail)
    {
        var request = new AssignmentConfirmations(_context.Localizer).ForRelease(detail.StudentName ?? string.Empty, detail.Number);
        if (detail.StudentId is { } student && await _context.Confirmations.ConfirmAsync(request))
        {
            await _release.RunAsync(student, detail.LockerId);
        }
    }

    /// <summary>One data-changing operation, run as a run-once command so it cannot run twice at the same time.</summary>
    sealed class Operation
    {
        readonly RunOnceCommand<string> _command;
        Guid _target;
        Guid _locker;

        public Operation(LockerDetailViewModel owner, Func<Guid, CancellationToken, Task<Result<string>>> run, string context)
        {
            _command = new RunOnceCommand<string>(
                (ct, _) => run(_target, ct), sentence => sentence, context, owner._context.Notifications, owner._context.Localizer,
                owner._context.Log, owner._context.Delay, () => owner._context.AfterWrite([_locker]));
        }

        public async Task RunAsync(Guid target, Guid locker)
        {
            if (_command.IsRunning)
            {
                return;
            }

            (_target, _locker) = (target, locker);
            await _command.RunAsync();
        }
    }
}
