// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Students;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Assigning;
using Arca.UI.Map;
using Arca.UI.Notifications;
using Arca.UI.Screens;

namespace Arca.UI.Lockers;

/// <summary>What the map needs to assign: the students without a locker, the check of a destination and the use cases that assign and change.</summary>
public sealed record LockerAssignmentServices(
    Func<CancellationToken, Task<Result<StudentListing>>> LoadStudentsWithoutLocker,
    Func<Guid, Guid, CancellationToken, Task<Result<AssignmentTargetCheck>>> CheckTarget,
    Func<AssignLockerRequest, CancellationToken, Task<Result<AssignLockerResult>>> Assign,
    Func<AssignLockerRequest, CancellationToken, Task<Result<AssignLockerResult>>> Change,
    Func<Guid, CancellationToken, Task<IReadOnlyList<string>>>? DebtLines = null);

/// <summary>
/// What the map view of the Lockers section adds to the lockers model (pantalles-taquilles-i-zones, Vista de mapa): the list of students
/// without a locker that is the origin of dragging, the destination check and the assignments that dragging, the menu and the keyboard all
/// send in the same way. The lockers themselves, their filters, their selection and their detail are the lockers model's, shared with the list.
/// </summary>
public sealed class LockersMapModel
{
    public LockersMapModel(
        LockersViewModel lockers, LockerAssignmentServices services, ResultNotifier notifier, IConfirmationService confirmations,
        ILocalizer localizer, INotificationService notifications, IErrorLog log, IDelay delay)
    {
        Lockers = lockers;
        Students = new StudentsWithoutLockerViewModel(services.LoadStudentsWithoutLocker, notifier, localizer);
        var texts = new AssignmentResultTexts(localizer);
        var assign = new AssignLockerInteraction(services.Assign, confirmations, localizer, notifications, log, delay, afterAssigned: lockers.RefreshAfterAssignmentAsync, debtDetails: services.DebtLines);
        var change = new AssignLockerInteraction(services.Change, confirmations, localizer, notifications, log, delay, texts.Changed, lockers.RefreshAfterAssignmentAsync, services.DebtLines);
        Drop = new AssignmentDropViewModel(services.CheckTarget, assign, localizer);
        lockers.OnPicked = change.AssignAsync;
        lockers.AssignStudent = assign.AssignAsync;
        lockers.ChosenStudent = () => Students.SelectedStudentId;
        lockers.AfterAssignmentChange = () => Students.LoadAsync();

        // The action of the list of students: put the chosen student in the chosen locker, the same as dragging.
        AssignToSelected = new AppAction("AssignToSelected", localizer.Get("Shell.Students.Menu.Assign"));
        AssignToSelected.Attach(
            () => _ = AssignSelectedAsync(assign),
            () => Students.NoActiveYear ? Availability.Unavailable(localizer.Get("Shell.Reason.NoYear"))
                : Students.SelectedStudentId is null ? Availability.Unavailable(localizer.Get("Shell.Reason.PickStudent"))
                : lockers.Lockers.Current is not { Status: Arca.Application.Search.LockerStatusView.Free }
                    ? Availability.Unavailable(localizer.Get("Shell.Students.PickFreeLocker"))
                    : Availability.Available);
        lockers.Lockers.CurrentChanged += (_, _) => AssignToSelected.Refresh();
        Students.PropertyChanged += (_, _) => AssignToSelected.Refresh();
    }

    public LockersViewModel Lockers { get; }

    public StudentsWithoutLockerViewModel Students { get; }

    public AssignmentDropViewModel Drop { get; }

    /// <summary>Assigns the student chosen in the list to the locker chosen on the map: the menu and keyboard way of doing what dragging does.</summary>
    public AppAction AssignToSelected { get; }

    public Task LoadAsync() => Students.LoadAsync();

    Task AssignSelectedAsync(AssignLockerInteraction assign) =>
        Students.SelectedStudentId is { } student && Lockers.Lockers.TryGetSelectedKey(out var locker)
            ? assign.AssignAsync(new AssignmentIntent(student, locker))
            : Task.CompletedTask;
}
