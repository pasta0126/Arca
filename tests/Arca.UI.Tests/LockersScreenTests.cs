// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.Lockers;
using Arca.Application.Lockers.AddLocker;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.GetLockerScreen;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Search;
using Arca.Application.Zones.ListZoneRows;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Actions;
using Arca.UI.Lists;
using Arca.UI.Lockers;
using Arca.UI.Screens;
using Xunit;

namespace Arca.UI.Tests;

public sealed class LockersScreenTests
{
    const string Spec = "pantalles-de-domini/pantalles-taquilles-i-zones";

    sealed class FakeForms : IFormDialogs
    {
        public List<IFormModel> Shown { get; } = [];

        public IFormModel Last => Shown[^1];

        public Task<bool> ShowAsync(IFormModel form)
        {
            Shown.Add(form);
            return Task.FromResult(false);
        }
    }

    sealed class FakeChoices : IChoiceDialogs
    {
        public Queue<string?> Answers { get; } = new();

        public List<ChoiceRequest> Asked { get; } = [];

        public Task<string?> ChooseAsync(ChoiceRequest request)
        {
            Asked.Add(request);
            return Task.FromResult(Answers.Count > 0 ? Answers.Dequeue() : null);
        }
    }

    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();
    readonly ResxLocalizer _localizer = new();
    readonly FakeForms _forms = new();
    readonly FakeChoices _choices = new();
    readonly List<LockerListRow> _rows = [];
    readonly List<ZoneRow> _zones = [];
    readonly List<string> _calls = [];
    RecordingConfirmations _confirmations = new(true);
    LockerEmptyState _emptyState = LockerEmptyState.None;
    int _refreshes;
    Guid _planZone;
    TaskCompletionSource? _gate;

    ZoneRow AddZone(string name, bool active = true, int lockers = 0, Error? deactivation = null, Error? deletion = null)
    {
        var zone = new ZoneRow(Guid.NewGuid(), name, active, lockers, deactivation, null, deletion);
        _zones.Add(zone);
        return zone;
    }

    LockerListRow AddLocker(int number, ZoneRow zone, LockerStatusView status = LockerStatusView.Free, string? student = null, bool debt = false)
    {
        var row = new LockerListRow(Guid.NewGuid(), number, zone.Id, zone.Name, status, student, debt, null, null);
        _rows.Add(row);
        return row;
    }

    static Error Refusal(string code) => new(code);

    LockerServices Services() => new(
        _ => Task.FromResult(Result<LockerRowsListing>.Success(new LockerRowsListing(
            [.. _rows.OrderBy(r => r.Number)],
            new LockerCounters(
                _rows.Count(r => r.Status != LockerStatusView.Retired), _rows.Count(r => r.Status == LockerStatusView.Free),
                _rows.Count(r => r.Status == LockerStatusView.Occupied), _rows.Count(r => r.Status == LockerStatusView.Broken),
                _rows.Count(r => r.Status == LockerStatusView.Maintenance), _rows.Count(r => r.Status == LockerStatusView.Reserved)),
            _emptyState))),
        (id, _) =>
        {
            var row = _rows.Single(r => r.Id == id);
            var occupied = row.Status == LockerStatusView.Occupied;
            var retired = row.Status == LockerStatusView.Retired;
            return Task.FromResult(Result<LockerScreenDetail>.Success(new LockerScreenDetail(
                row, retired ? Refusal("Lockers.Retired") : null, row.Status == LockerStatusView.Free ? null : Refusal("Lockers.NotFree"),
                row.Status == LockerStatusView.Reserved ? null : Refusal("Lockers.NotReserved"),
                row.Status == LockerStatusView.Broken ? Refusal("Lockers.AlreadyOutOfService") : null, null,
                row.Status is LockerStatusView.Broken or LockerStatusView.Maintenance ? null : Refusal("Lockers.NotOutOfService"),
                occupied ? Refusal("Lockers.HasAssignment") : null)));
        },
        (_, _) => Task.FromResult(Result<IReadOnlyList<string>>.Success(["alta"])),
        _ => Task.FromResult(Result<IReadOnlyList<ZoneRow>>.Success([.. _zones.OrderBy(z => z.Name)])),
        (request, _) =>
        {
            if (_rows.Any(r => r.Number == request.Number && r.Status != LockerStatusView.Retired))
            {
                return Task.FromResult(Result<string>.Failure(new Error("Lockers.NumberInUse", Args: [request.Number])));
            }

            _calls.Add($"add {request.Number}");
            var zone = _zones.Single(z => z.Id == request.ZoneId);
            AddLocker(request.Number, zone);
            return Task.FromResult(Result<string>.Success($"Taquilla {request.Number} creada."));
        },
        (request, _, _) => Task.FromResult(Result<CreateLockerRangePlan>.Success(Plan(request))),
        (plan, _, _) =>
        {
            _calls.Add($"range {plan.First}-{plan.Last}");
            return Task.FromResult(Result<CreateLockerRangeResult>.Success(new CreateLockerRangeResult(true, plan.ToCreate.Count, plan)));
        },
        result => $"{result.Created} taquilles creades.",
        (id, number, _) =>
        {
            _calls.Add($"number {number}");
            return Task.FromResult(Result<string>.Success("Número canviat."));
        },
        (id, zone, _) =>
        {
            _calls.Add("zone");
            return Task.FromResult(Result<string>.Success("Zona canviada."));
        },
        (id, note, _) =>
        {
            _calls.Add($"reserve {note}");
            return Task.FromResult(Result<string>.Success("Reservada."));
        },
        (id, _) =>
        {
            _calls.Add("remove reservation");
            return Task.FromResult(Result<string>.Success("Reserva treta."));
        },
        (id, kind, decision, target, confirm, _) =>
        {
            _calls.Add($"out {kind} {decision} {target is not null} {confirm}");
            return Task.FromResult(Result<OutOfServiceView>.Success(new OutOfServiceView("Fora de servei.", [], [])));
        },
        async (id, _) =>
        {
            if (_gate is not null)
            {
                await _gate.Task;
            }

            _calls.Add("restore");
            return Result<string>.Success("Reparada.");
        },
        (id, _) =>
        {
            _calls.Add("retire");
            return Task.FromResult(Result<string>.Success("De baixa."));
        },
        (name, _) =>
        {
            if (_zones.Any(z => string.Equals(z.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(Result<string>.Failure(new Error("Zones.NameDuplicate", Args: [name])));
            }

            AddZone(name);
            return Task.FromResult(Result<string>.Success($"Zona {name} creada."));
        },
        (id, name, _) => Task.FromResult(Result<string>.Success("Zona reanomenada.")),
        async (id, _) =>
        {
            if (_gate is not null)
            {
                await _gate.Task;
            }

            _calls.Add("deactivate zone");
            return Result<string>.Success("Zona desactivada.");
        },
        (id, _) => Task.FromResult(Result<string>.Success("Zona reactivada.")),
        (id, _) =>
        {
            _calls.Add("delete zone");
            _zones.RemoveAll(z => z.Id == id);
            return Task.FromResult(Result<string>.Success("Zona eliminada."));
        });

    CreateLockerRangePlan Plan(CreateLockerRangeRequest request)
    {
        _planZone = request.ZoneId;
        var numbers = Enumerable.Range(request.First, request.Last - request.First + 1).ToList();
        var taken = numbers.Where(n => _rows.Any(r => r.Number == n)).ToList();
        return new CreateLockerRangePlan(request.First, request.Last, request.ZoneId, "Planta 1", [.. numbers.Except(taken)], taken);
    }

    ScreenContext Context() => new(
        _localizer, _notifications, _log, _delay, _confirmations, _forms, () =>
        {
            _refreshes++;
            return Task.CompletedTask;
        }, _choices);

    LockersViewModel Model(Func<Task>? openNewZone = null) =>
        new(Services(), Context(), new ActionRegistry(_localizer, UiPlatform.Windows)[StandardActions.New], openNewZone ?? (() => Task.CompletedTask), new Arca.UI.Assigning.AssignmentDialogs(null!, Context()));

    ZonesViewModel Zones() => new(Services(), Context(), () => Task.CompletedTask);

    // --- The list ---

    [Fact]
    [Trait("spec", Spec + ": Lista de taquillas con filtros y contadores (Lista por defecto)")]
    public async Task The_list_shows_the_active_lockers_by_number_with_the_counters_and_without_the_retired()
    {
        var zone = AddZone("Planta 1");
        AddLocker(2, zone);
        AddLocker(1, zone, LockerStatusView.Occupied, "Marta Puig", debt: true);
        AddLocker(3, zone, LockerStatusView.Retired);
        var model = Model();

        await model.LoadAsync();

        Assert.Equal([1, 2], model.Lockers.List.Rows.Select(r => r.Number));
        Assert.Contains("2 actives", model.CountersText, StringComparison.Ordinal);
        Assert.Contains("1 lliures", model.CountersText, StringComparison.Ordinal);
        Assert.Contains("1 ocupades", model.CountersText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Lista de taquillas con filtros y contadores (Incluir bajas)")]
    public async Task Including_the_retired_shows_them_told_apart_with_text()
    {
        var zone = AddZone("Planta 1");
        AddLocker(1, zone);
        AddLocker(2, zone, LockerStatusView.Retired);
        var model = Model();
        await model.LoadAsync();

        model.IncludeRetired = true;

        Assert.Equal(2, model.Lockers.List.Rows.Count);
        var column = model.Lockers.List.Columns.Single(c => c.Id == "status");
        Assert.Equal("De baixa", column.Text(model.Lockers.List.Rows[1]));
    }

    [Fact]
    [Trait("spec", Spec + ": Lista de taquillas con filtros y contadores (Lista por defecto)")]
    public async Task The_zone_status_and_number_filters_narrow_300_lockers_and_the_counters_do_not_change()
    {
        var first = AddZone("Planta 1");
        var second = AddZone("Planta 2");
        for (var i = 1; i <= 300; i++)
        {
            AddLocker(i, i <= 150 ? first : second, i % 3 == 0 ? LockerStatusView.Occupied : LockerStatusView.Free, i % 3 == 0 ? "Alumne " + i : null);
        }

        var model = Model();
        await model.LoadAsync();
        Assert.Equal(300, model.Lockers.List.Rows.Count);

        model.ZoneFilter = second.Id.ToString();
        Assert.Equal(150, model.Lockers.List.Rows.Count);
        model.StatusFilter = nameof(LockerStatusView.Occupied);
        Assert.Equal(50, model.Lockers.List.Rows.Count);
        model.NumberFilter = "153";
        Assert.Equal([153], model.Lockers.List.Rows.Select(r => r.Number));
        Assert.Contains("300 actives", model.CountersText, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Lista de taquillas con filtros y contadores (Filtro sin resultados)")]
    public async Task A_filter_with_no_match_says_so_and_offers_to_clear_the_filters()
    {
        var zone = AddZone("Planta 1");
        AddLocker(1, zone);
        var model = Model();
        await model.LoadAsync();

        model.NumberFilter = "99";
        Assert.Equal(ListViewState.NoResults, model.Lockers.State.State);
        model.ClearFilters();

        Assert.Equal(ListViewState.Content, model.Lockers.State.State);
        Assert.Single(model.Lockers.List.Rows);
    }

    [Fact]
    [Trait("spec", Spec + ": Lista de taquillas con filtros y contadores (Sin taquillas)")]
    public async Task Without_lockers_the_empty_state_offers_a_range_and_a_single_locker_and_without_zones_a_zone()
    {
        _emptyState = LockerEmptyState.NoZones;
        var zoneOpened = 0;
        var model = Model(() =>
        {
            zoneOpened++;
            return Task.CompletedTask;
        });
        await model.LoadAsync();
        Assert.Contains("zona", model.Lockers.State.Message, StringComparison.OrdinalIgnoreCase);
        model.Lockers.State.Actions.Single().Command.Execute(null);
        Assert.Equal(1, zoneOpened);

        _emptyState = LockerEmptyState.NoLockers;
        await model.LoadAsync();
        Assert.Equal(2, model.Lockers.State.Actions.Count);
    }

    // --- The detail ---

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla (Taquilla libre)")]
    public async Task A_free_locker_offers_every_action_and_an_occupied_one_says_why_it_cannot_be_retired()
    {
        var zone = AddZone("Planta 1");
        var free = AddLocker(1, zone);
        var busy = AddLocker(2, zone, LockerStatusView.Occupied, "Marta Puig");
        var model = Model();
        await model.LoadAsync();

        await model.Detail.ShowAsync(true, free.Id);
        Assert.Equal(["Assign", "Edit", "Reserve", "RemoveReservation", "MarkBroken", "MarkMaintenance", "Restore", "Retire"], model.Detail.Actions.Select(a => a.Id));
        Assert.True(model.Detail.Actions.Single(a => a.Id == "Reserve").IsAvailable);
        Assert.False(model.Detail.Actions.Single(a => a.Id == "Restore").IsAvailable);

        await model.Detail.ShowAsync(true, busy.Id);
        var retire = model.Detail.Actions.Single(a => a.Id == "Retire");
        Assert.False(retire.IsAvailable);
        Assert.Equal(_localizer.Message(new Error("Lockers.HasAssignment")), retire.UnavailableReason);
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla (Taquilla de baja)")]
    public async Task A_retired_locker_offers_no_action_that_changes_it_but_keeps_its_history()
    {
        var zone = AddZone("Planta 1");
        var retired = AddLocker(1, zone, LockerStatusView.Retired);
        var model = Model();
        await model.LoadAsync();

        await model.Detail.ShowAsync(true, retired.Id);
        await model.Detail.LoadHistoryAsync();

        Assert.Empty(model.Detail.Actions);
        Assert.Equal(["alta"], model.Detail.History);
    }

    // --- Adding ---

    [Fact]
    [Trait("spec", Spec + ": Alta individual desde un formulario (Alta correcta)")]
    public async Task Adding_a_locker_creates_it_and_the_form_stays_open_with_the_next_number()
    {
        var zone = AddZone("Planta 1");
        AddLocker(14, zone);
        var model = Model();
        await model.LoadAsync();
        await model.NewLockerAsync();
        var form = (FormViewModel<string>)_forms.Last;
        Assert.Equal("15", form.Fields.Single(f => f.Id == "Number").Text); // the one after the last
        Assert.True(form.StaysOpen);

        await form.Save.RunAsync();

        Assert.Contains("add 15", _calls);
        Assert.Equal("16", form.Fields.Single(f => f.Id == "Number").Text);
        Assert.Equal(zone.Id.ToString(), form.Fields.Single(f => f.Id == "Zone").Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta individual desde un formulario (Número repetido)")]
    public async Task A_repeated_number_marks_the_field_and_keeps_what_was_written()
    {
        var zone = AddZone("Planta 1");
        AddLocker(15, zone);
        var model = Model();
        await model.LoadAsync();
        await model.NewLockerAsync();
        var form = (FormViewModel<string>)_forms.Last;
        form.Fields.Single(f => f.Id == "Number").Text = "15";

        await form.Save.RunAsync();

        Assert.True(form.Fields.Single(f => f.Id == "Number").HasError);
        Assert.Equal("15", form.Fields.Single(f => f.Id == "Number").Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta individual desde un formulario (Sin zonas activas)")]
    public async Task Without_an_active_zone_the_form_explains_it_and_offers_to_create_one()
    {
        var opened = 0;
        var model = Model(() =>
        {
            opened++;
            return Task.CompletedTask;
        });
        _choices.Answers.Enqueue("zone");

        await model.NewLockerAsync();

        Assert.Empty(_forms.Shown);
        Assert.Single(_choices.Asked);
        Assert.Equal(1, opened);
    }

    // --- Ranges ---

    [Fact]
    [Trait("spec", Spec + ": Alta por rangos con vista previa (Vista previa)")]
    public async Task A_range_shows_how_many_will_be_created_and_creates_them_only_after_confirming()
    {
        var zone = AddZone("Planta 1");
        var model = Model();
        await model.NewRangeAsync();
        var form = (FormViewModel<string>)_forms.Last;
        form.Fields.Single(f => f.Id == "First").Text = "1";
        form.Fields.Single(f => f.Id == "Last").Text = "40";

        await form.Save.RunAsync();

        Assert.Contains("40", _confirmations.Asked.Single().Consequence, StringComparison.Ordinal);
        Assert.Contains("range 1-40", _calls);
        Assert.Equal(zone.Id, _planZone);
        Assert.Equal("40 taquilles creades.", _notifications.Published.Single().Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta por rangos con vista previa (Conflicto en el rango)")]
    public async Task A_conflict_in_the_range_is_shown_and_nothing_is_created()
    {
        var zone = AddZone("Planta 1");
        AddLocker(5, zone);
        var model = Model();
        await model.NewRangeAsync();
        var form = (FormViewModel<string>)_forms.Last;
        form.Fields.Single(f => f.Id == "First").Text = "1";
        form.Fields.Single(f => f.Id == "Last").Text = "10";

        await form.Save.RunAsync();

        Assert.Contains("5", form.Note, StringComparison.Ordinal);
        Assert.Empty(_confirmations.Asked);
        Assert.DoesNotContain(_calls, c => c.StartsWith("range", StringComparison.Ordinal));
        Assert.Empty(_notifications.Published);
    }

    [Fact]
    [Trait("spec", Spec + ": Alta por rangos con vista previa (Progreso)")]
    public async Task Saving_a_range_twice_at_once_creates_it_once()
    {
        AddZone("Planta 1");
        var model = Model();
        await model.NewRangeAsync();
        var form = (FormViewModel<string>)_forms.Last;
        form.Fields.Single(f => f.Id == "First").Text = "1";
        form.Fields.Single(f => f.Id == "Last").Text = "5";

        var first = form.Save.RunAsync();
        await form.Save.RunAsync();
        await first;

        Assert.Single(_calls, c => c.StartsWith("range", StringComparison.Ordinal));
    }

    // --- Out of service ---

    [Fact]
    [Trait("spec", Spec + ": Fuera de servicio con decisión (Taquilla libre averiada)")]
    public async Task A_free_locker_is_marked_broken_at_once()
    {
        var zone = AddZone("Planta 1");
        var free = AddLocker(1, zone);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, free.Id);

        model.Detail.Actions.Single(a => a.Id == "MarkBroken").Execute(null);
        await Task.Delay(100);

        Assert.Contains("out Broken  False False", _calls);
        Assert.Empty(_choices.Asked);
    }

    [Fact]
    [Trait("spec", Spec + ": Fuera de servicio con decisión (Taquilla ocupada)")]
    public async Task An_occupied_locker_asks_for_the_decision_naming_the_student_and_changes_nothing_until_it_is_made()
    {
        var zone = AddZone("Planta 1");
        var busy = AddLocker(1, zone, LockerStatusView.Occupied, "Marta Puig");
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, busy.Id);
        _choices.Answers.Enqueue(nameof(OutOfServiceDecisionView.Release));

        model.Detail.Actions.Single(a => a.Id == "MarkBroken").Execute(null);
        await Task.Delay(150);

        var asked = _choices.Asked.Single();
        Assert.Contains("Marta Puig", asked.Message, StringComparison.Ordinal);
        Assert.Equal(3, asked.Options.Count);
        Assert.Contains("out Broken Release False False", _calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Fuera de servicio con decisión (Cancelar la decisión)")]
    public async Task Closing_the_decision_without_choosing_leaves_the_locker_and_its_assignment_as_they_were()
    {
        var zone = AddZone("Planta 1");
        var busy = AddLocker(1, zone, LockerStatusView.Occupied, "Marta Puig");
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, busy.Id);
        _choices.Answers.Enqueue(null);

        model.Detail.Actions.Single(a => a.Id == "MarkBroken").Execute(null);
        await Task.Delay(150);

        Assert.Empty(_calls);
        Assert.Empty(_notifications.Published);
    }

    [Fact]
    [Trait("spec", Spec + ": Fuera de servicio con decisión (Taquilla ocupada)")]
    public async Task Choosing_to_reassign_asks_for_a_free_destination_and_reassigns_to_it()
    {
        var zone = AddZone("Planta 1");
        var busy = AddLocker(1, zone, LockerStatusView.Occupied, "Marta Puig");
        var free = AddLocker(2, zone);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, busy.Id);
        _choices.Answers.Enqueue(nameof(OutOfServiceDecisionView.Reassign));

        model.Detail.Actions.Single(a => a.Id == "MarkBroken").Execute(null);
        await Task.Delay(150);
        var form = (FormViewModel<OutOfServiceView>)_forms.Last;
        Assert.Equal([free.Id.ToString()], form.Fields.Single().Options!.Select(o => o.Id));
        form.Fields.Single().Text = free.Id.ToString();
        await form.Save.RunAsync();

        Assert.Contains("out Broken Reassign True False", _calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Fuera de servicio con decisión (Reparada)")]
    public async Task A_locker_out_of_service_can_be_marked_repaired()
    {
        var zone = AddZone("Planta 1");
        var broken = AddLocker(1, zone, LockerStatusView.Broken);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, broken.Id);

        model.Detail.Actions.Single(a => a.Id == "Restore").Execute(null);
        await Task.Delay(100);

        Assert.Contains("restore", _calls);
        Assert.Equal("Reparada.", _notifications.Published.Single().Text);
    }

    // --- Retiring ---

    [Fact]
    [Trait("spec", Spec + ": Baja de una taquilla con confirmación (Confirmar la baja)")]
    public async Task Retiring_a_locker_says_it_cannot_be_undone_and_only_retires_when_confirmed()
    {
        var zone = AddZone("Planta 1");
        var free = AddLocker(1, zone);
        _confirmations = new RecordingConfirmations(false);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, free.Id);

        model.Detail.Actions.Single(a => a.Id == "Retire").Execute(null);
        await Task.Delay(100);
        Assert.True(_confirmations.Asked.Single().Destructive);
        Assert.DoesNotContain("retire", _calls);

        _confirmations = new RecordingConfirmations(true);
        var again = Model();
        await again.LoadAsync();
        await again.Detail.ShowAsync(true, free.Id);
        again.Detail.Actions.Single(a => a.Id == "Retire").Execute(null);
        await Task.Delay(100);
        Assert.Contains("retire", _calls);
    }

    // --- Zones ---

    [Fact]
    [Trait("spec", Spec + ": Vista de zonas (Desactivar con taquillas, Eliminar zona con historial)")]
    public async Task A_zone_with_lockers_cannot_be_deactivated_and_one_with_history_cannot_be_deleted_and_each_says_why()
    {
        var zone = AddZone("Planta 1", lockers: 3, deactivation: new Error("Zones.HasActiveLockers", Args: [3]), deletion: new Error("Zones.HasHistory"));
        var model = Zones();
        await model.LoadAsync();

        await model.Detail.ShowAsync(true, zone.Id);

        var deactivate = model.Detail.Actions.Single(a => a.Id == "Deactivate");
        Assert.False(deactivate.IsAvailable);
        Assert.Contains("3", deactivate.UnavailableReason, StringComparison.Ordinal);
        Assert.False(model.Detail.Actions.Single(a => a.Id == "Delete").IsAvailable);
    }

    [Fact]
    [Trait("spec", Spec + ": Vista de zonas (Crear zona, Nombre repetido)")]
    public async Task Creating_a_zone_lists_it_and_a_repeated_name_is_marked_in_the_field()
    {
        AddZone("Planta 1");
        var model = Zones();
        await model.LoadAsync();

        await model.NewZoneAsync();
        var form = (FormViewModel<string>)_forms.Last;
        form.Fields.Single().Text = "planta 1";
        await form.Save.RunAsync();
        Assert.True(form.Fields.Single().HasError);

        form.Fields.Single().Text = "Planta 2";
        await form.Save.RunAsync();
        Assert.Equal(["Planta 1", "Planta 2"], model.Zones.List.Rows.Select(z => z.Name));
    }

    [Fact]
    [Trait("spec", Spec + ": Vista de zonas (Sin zonas)")]
    public async Task Without_zones_the_view_explains_it_and_offers_to_create_the_first()
    {
        var model = Zones();

        await model.LoadAsync();

        Assert.Equal(ListViewState.Empty, model.Zones.State.State);
        Assert.Equal(model.NewZone.Label, model.Zones.State.Actions.Single().Label);
    }

    [Fact]
    [Trait("spec", Spec + ": Vista de zonas (Eliminar zona)")]
    public async Task Deleting_a_zone_asks_first_and_removes_it()
    {
        var zone = AddZone("Planta 1");
        var model = Zones();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, zone.Id);

        model.Detail.Actions.Single(a => a.Id == "Delete").Execute(null);
        await Task.Delay(100);

        Assert.True(_confirmations.Asked.Single().Destructive);
        Assert.Contains("delete zone", _calls);
        Assert.Empty(model.Zones.List.Rows);
    }

    // --- Double execution ---

    [Fact]
    [Trait("spec", "pantalles-de-domini/tasks: 7.2 Protección contra doble ejecución en todos los guardados y operaciones")]
    public async Task Pressing_an_action_of_a_detail_twice_at_once_acts_once_and_notifies_once()
    {
        var zone = AddZone("Planta 1");
        var broken = AddLocker(1, zone, LockerStatusView.Broken);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, broken.Id);
        _gate = new TaskCompletionSource();

        var restore = model.Detail.Actions.Single(a => a.Id == "Restore");
        restore.Execute(null);
        restore.Execute(null); // the second click while the first is still going
        _gate.SetResult();
        await Task.Delay(150);

        Assert.Single(_calls, c => c == "restore");
        Assert.Single(_notifications.Published);
    }

    [Fact]
    [Trait("spec", "pantalles-de-domini/tasks: 7.2 Protección contra doble ejecución en todos los guardados y operaciones")]
    public async Task Pressing_a_zone_operation_twice_at_once_acts_once()
    {
        var zone = AddZone("Planta 1");
        var model = Zones();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, zone.Id);
        _gate = new TaskCompletionSource();

        var deactivate = model.Detail.Actions.Single(a => a.Id == "Deactivate");
        deactivate.Execute(null);
        deactivate.Execute(null);
        _gate.SetResult();
        await Task.Delay(150);

        Assert.Single(_calls, c => c == "deactivate zone");
    }

    [Avalonia.Headless.XUnit.AvaloniaFact]
    public async Task Screenshot_of_the_lockers_section()
    {
        var zone = AddZone("Planta 1");
        AddZone("Planta 2");
        for (var i = 1; i <= 24; i++)
        {
            AddLocker(i, zone, i % 4 == 0 ? LockerStatusView.Occupied : i == 7 ? LockerStatusView.Broken : LockerStatusView.Free, i % 4 == 0 ? "Alumne " + i : null, debt: i == 8);
        }

        var model = Model();
        var screen = Arca.UI.Lockers.LockersView.Create(model, _localizer);
        var window = new Avalonia.Controls.Window { Content = screen, Width = 1200, Height = 700 };
        window.Show();
        await model.LoadAsync();
        model.Lockers.Select(model.Lockers.List.Rows[7]);
        await Task.Delay(200);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        ScreenshotTests.Take(window, "lockers");
        window.Close();
    }
}
