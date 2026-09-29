// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.AssignLocker;
using Arca.Application.LockerMap;
using Arca.Application.Localization;
using Arca.Application.Preferences;
using Arca.Application.Search;
using Arca.Application.Students;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Assigning;
using Arca.UI.Map;
using Arca.UI.Notifications;
using Arca.UI.Preferences;
using Arca.UI.Search;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>Answers that arrive late, and failures outside a command, must never leave the screen showing or doing the wrong thing.</summary>
public sealed class StartScreenRaceTests
{
    const string Spec = "ui-shell/pantalla-principal: Carga y rendimiento del mapa";

    sealed class MemoryStore : IUiPreferencesStore
    {
        public UiPreferences Load() => new();

        public void Save(UiPreferences preferences)
        {
        }
    }

    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();

    static readonly Guid _a = Guid.NewGuid();
    static readonly Guid _b = Guid.NewGuid();

    static LockerDetail Detail(Guid id, int number) => new(id, number, "Planta 1", LockerStatusView.Free, null, null, null, null, null, null, false, 0m);

    [Fact]
    [Trait("spec", Spec + " (Actualización tras un cambio)")]
    public async Task A_detail_that_arrives_late_does_not_replace_the_locker_chosen_afterwards()
    {
        var slow = new TaskCompletionSource<Result<LockerDetail?>>();
        Task<Result<string>> Op(Guid id, CancellationToken ct) => Task.FromResult(Result<string>.Success("fet"));
        var notifier = new ResultNotifier(_notifications, _localizer, _log);
        var students = new StudentsWithoutLockerViewModel(_ => Task.FromResult(Result<StudentListing>.Success(new([], new(0, 0, 0)))), notifier, _localizer);
        var map = new LockerMapViewModel(_ => Task.FromResult(Result<LockerMapData>.Success(LockerMapData.Empty)), (_, _) => Task.FromResult(Result<MapLocker?>.Success(null)), new UiPreferencesSession(new MemoryStore()), notifier, _localizer);
        var assign = new AssignLockerInteraction((_, _) => Task.FromResult(Result<AssignLockerResult>.Failure(new Error("X.Y"))), new RecordingConfirmations(true), _localizer, _notifications, _log, _delay);
        var detail = new LockerDetailViewModel(new LockerDetailContext(
            (id, _) => id == _a ? slow.Task : Task.FromResult(Result<LockerDetail?>.Success(Detail(_b, 2))),
            new LockerOperations(Op, Op, Op, Op, Op), assign, new RecordingConfirmations(true), _localizer, _notifications, _log, _delay, students, map, _ => Task.CompletedTask));

        var first = detail.ShowAsync(_a); // the person clicks A…
        await detail.ShowAsync(_b); // …and then B, which answers at once
        slow.SetResult(Result<LockerDetail?>.Success(Detail(_a, 1))); // A's answer arrives late
        await first;

        Assert.Equal(_b, detail.Detail!.LockerId);
        Assert.All(detail.Actions, a => Assert.Equal(_b, detail.Detail.LockerId)); // and the actions are B's
    }

    [Fact]
    [Trait("spec", "ui-shell/navegacio-i-cerca: Búsqueda sin bloquear (Escritura rápida)")]
    public async Task A_search_that_fails_after_a_newer_one_started_does_not_close_the_panel_of_the_newer_one()
    {
        var release = new TaskCompletionSource<Result<GlobalSearchResult>>();
        var search = new GlobalSearchViewModel(
            (request, _) => request.Text == "ga" ? release.Task : Task.FromResult(Result<GlobalSearchResult>.Success(GlobalSearchResult.Empty)),
            _delay, new ResultNotifier(_notifications, _localizer, _log), new SearchNavigator(), _localizer);

        search.Text = "ga";
        await Task.Delay(20);
        _delay.Elapse(GlobalSearchViewModel.Pause + TimeSpan.FromMilliseconds(1)); // the first search is waiting for its answer
        await Task.Delay(20);
        search.Text = "gar"; // a newer one starts and is shown as "searching"
        await Task.Delay(20);
        release.SetException(new IOException("disk failed")); // the old one fails now
        await Task.Delay(40);

        Assert.Equal(SearchState.Searching, search.State); // the newer search keeps its panel
        Assert.Empty(_notifications.Published); // and nobody is told about a search that no longer matters
    }

    [Fact]
    [Trait("spec", "ui-shell/pantalla-principal: Detalle de la taquilla seleccionada (Taquilla ocupada)")]
    public async Task A_failure_of_the_change_outside_its_command_is_told_to_the_person()
    {
        var notifier = new ResultNotifier(_notifications, _localizer, _log);
        var map = new LockerMapViewModel(
            _ => Task.FromResult(Result<LockerMapData>.Success(new([new ZoneMap(Guid.NewGuid(), "P", [new MapLocker(_a, 1, LockerStatusView.Free, null, null, false)], new(1, 1, 0, 0, 0, 0))], new(1, 1, 0, 0, 0, 0)))),
            (_, _) => Task.FromResult(Result<MapLocker?>.Success(null)), new UiPreferencesSession(new MemoryStore()), notifier, _localizer)
        {
            OnPicked = _ => throw new InvalidOperationException("boom"),
        };
        await map.LoadAsync();
        map.BeginPick(Guid.NewGuid(), "Marta Puig");

        map.Select(_a);
        await Task.Delay(40);

        Assert.Equal("ChangeLocker", Assert.Single(_log.Entries).Context);
        Assert.Equal(Arca.Application.Feedback.NotificationKind.Error, Assert.Single(_notifications.Published).Kind);
    }

    [Fact]
    [Trait("spec", "ui-shell/pantalla-principal: Filtros del mapa (Resaltar una búsqueda)")]
    public async Task Choosing_a_locker_in_the_search_while_changing_a_locker_ends_the_change_first()
    {
        var notifier = new ResultNotifier(_notifications, _localizer, _log);
        var map = new LockerMapViewModel(
            _ => Task.FromResult(Result<LockerMapData>.Success(new([new ZoneMap(Guid.NewGuid(), "P", [new MapLocker(_a, 1, LockerStatusView.Free, null, null, false)], new(1, 1, 0, 0, 0, 0))], new(1, 1, 0, 0, 0, 0)))),
            (_, _) => Task.FromResult(Result<MapLocker?>.Success(null)), new UiPreferencesSession(new MemoryStore()), notifier, _localizer);
        await map.LoadAsync();
        map.BeginPick(Guid.NewGuid(), "Marta Puig");

        map.Reveal(_a);

        Assert.Null(map.Picking); // the person moved on: the next click must not reassign the student
        Assert.Equal(_a, map.SelectedLockerId);
    }
}
