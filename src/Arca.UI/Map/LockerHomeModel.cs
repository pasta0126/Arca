// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Common;
using Arca.Application.Feedback;
using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Domain.Common;
using Arca.UI.Actions;
using Arca.UI.Assigning;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Shell;

namespace Arca.UI.Map;

/// <summary>Everything the start screen reads and does, as delegates the composition of the application supplies.</summary>
public sealed record LockerHomeServices(
    Func<CancellationToken, Task<Result<LockerMapData>>> LoadMap,
    Func<Guid, CancellationToken, Task<Result<MapLocker?>>> LoadMapLocker,
    Func<Guid, CancellationToken, Task<Result<LockerDetail?>>> LoadDetail,
    Func<CancellationToken, Task<Result<StudentListing>>> LoadStudentsWithoutLocker,
    Func<Guid, Guid, CancellationToken, Task<Result<AssignmentTargetCheck>>> CheckTarget,
    Func<AssignLockerRequest, CancellationToken, Task<Result<AssignLockerResult>>> Assign,
    Func<AssignLockerRequest, CancellationToken, Task<Result<AssignLockerResult>>> Change,
    LockerOperations Operations);

/// <summary>
/// The start screen without any window: the map, the detail of the chosen locker, the panel of students without a locker and
/// dragging, joined so that after every change only what changed is read again (ui-shell, Actualización tras un cambio): the
/// locker or lockers involved and the counters, the list of students, the detail and the global state, never the whole map.
/// </summary>
public sealed class LockerHomeModel
{
    readonly LockerHomeServices _services;
    readonly GlobalStateService _state;

    public LockerHomeModel(
        LockerHomeServices services, UiPreferencesSession preferences, ResultNotifier notifier, IConfirmationService confirmations,
        ILocalizer localizer, INotificationService notifications, IErrorLog log, IDelay delay, GlobalStateService state)
    {
        _services = services;
        _state = state;
        Map = new LockerMapViewModel(services.LoadMap, services.LoadMapLocker, preferences, notifier, localizer);
        Students = new StudentsWithoutLockerViewModel(services.LoadStudentsWithoutLocker, notifier, localizer);

        var texts = new AssignmentResultTexts(localizer);
        var assign = new AssignLockerInteraction(services.Assign, confirmations, localizer, notifications, log, delay, afterAssigned: AfterAssignedAsync);
        var change = new AssignLockerInteraction(services.Change, confirmations, localizer, notifications, log, delay, texts.Changed, AfterAssignedAsync);
        Drop = new AssignmentDropViewModel(services.CheckTarget, assign, localizer);
        Map.OnPicked = change.AssignAsync;
        Detail = new LockerDetailViewModel(new LockerDetailContext(
            services.LoadDetail, services.Operations, assign, confirmations, localizer, notifications, log, delay, Students, Map, AfterWriteAsync));

        // The action of the list of students: put the chosen student in the chosen locker, the same as dragging.
        AssignToSelected = new AppAction("AssignToSelected", localizer.Get("Shell.Students.Menu.Assign"));
        AssignToSelected.Attach(
            () => _ = AssignSelectedAsync(assign),
            () => Students.NoActiveYear ? Availability.Unavailable(localizer.Get("Shell.Reason.NoYear"))
                : Students.SelectedStudentId is null ? Availability.Unavailable(localizer.Get("Shell.Reason.PickStudent"))
                : Map.SelectedLockerId is not { } id || Map.Find(id)?.Status != LockerStatusView.Free
                    ? Availability.Unavailable(localizer.Get("Shell.Students.PickFreeLocker"))
                    : Availability.Available);
        Map.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LockerMapViewModel.SelectedLockerId))
            {
                _ = Detail.ShowAsync(Map.SelectedLockerId);
                AssignToSelected.Refresh();
            }
        };
        Students.PropertyChanged += (_, _) => AssignToSelected.Refresh();
    }

    public LockerMapViewModel Map { get; }

    public StudentsWithoutLockerViewModel Students { get; }

    public LockerDetailViewModel Detail { get; }

    public AssignmentDropViewModel Drop { get; }

    /// <summary>Assigns the student chosen in the list to the locker chosen on the map: the menu and keyboard way of doing what dragging does.</summary>
    public AppAction AssignToSelected { get; }

    /// <summary>Loads the map and the list of students.</summary>
    public async Task LoadAsync()
    {
        await Task.WhenAll(Map.LoadAsync(), Students.LoadAsync());
    }

    Task AssignSelectedAsync(AssignLockerInteraction assign) =>
        Students.SelectedStudentId is { } student && Map.SelectedLockerId is { } locker
            ? assign.AssignAsync(new AssignmentIntent(student, locker))
            : Task.CompletedTask;

    /// <summary>After an assignment or a change: the new locker and, if there was one, the old one, which changed too.</summary>
    Task AfterAssignedAsync(AssignmentIntent intent)
    {
        var old = Map.LockerHeldBy(intent.StudentId)?.LockerId;
        return AfterWriteAsync(old is { } previous && previous != intent.LockerId ? [intent.LockerId, previous] : [intent.LockerId]);
    }

    /// <summary>Reads again what a change touched, and nothing else.</summary>
    async Task AfterWriteAsync(IEnumerable<Guid> lockerIds)
    {
        foreach (var id in lockerIds)
        {
            await Map.RefreshLockerAsync(id);
        }

        await Students.LoadAsync();
        await Detail.ShowAsync(Map.SelectedLockerId);
        await _state.RefreshAsync();
        AssignToSelected.Refresh();
    }
}
