// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges.GetStudentChargesScreen;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Zones.ListZoneRows;
using Arca.Domain.Charges;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Actions;
using Arca.UI.Charges;
using Arca.UI.Lists;
using Arca.UI.Screens;
using Xunit;

namespace Arca.UI.Tests;

/// <summary>The charges of the students and their debtors, in memory, for the tests of the screens.</summary>
internal sealed class FakeChargeWorld
{
    public readonly Guid YearNow = Guid.NewGuid();
    public readonly Guid YearBefore = Guid.NewGuid();
    public readonly List<ChargeLine> Lines = [];
    public readonly List<string> Calls = [];
    public decimal? KeyAmount = 10m;
    public Error? KeyBlocked;
    public DateOnly Today = new(2026, 9, 29);
    public TaskCompletionSource? Gate;

    public static readonly Error Blocked = new("Charges.InvalidStatus");

    public ChargeLine Line(string concept, string status, decimal amount, bool current = true, string? reason = null) => AddLine(new ChargeLine(
        Guid.NewGuid(), concept, status, current ? YearNow : YearBefore, current ? "2026-2027" : "2025-2026", current, amount, null, reason,
        status == "Pending" ? null : Blocked, status == "Pending" ? null : Blocked, status == "Pending" ? null : Blocked,
        status == "Voided" ? Blocked : null, status == "Pending" ? null : Blocked, status is "Pending" or "Voided" ? Blocked : null));

    ChargeLine AddLine(ChargeLine line)
    {
        Lines.Add(line);
        return line;
    }

    public ChargeServices Services() => new(
        async (id, _) =>
        {
            if (Gate is not null)
            {
                await Gate.Task;
            }

            var pending = Lines.Where(l => l.Status == "Pending").Sum(l => l.Amount);
            return Result<StudentChargesScreen>.Success(new StudentChargesScreen(
                id, "Marta Puig", pending == 0, false, pending, [.. Lines], YearNow, "2026-2027", KeyBlocked, KeyAmount));
        },
        (_, _) => Task.FromResult(Result<IReadOnlyList<string>>.Success(["Marcat com a pagat."])),
        (id, date, _) =>
        {
            if (date > Today)
            {
                return Task.FromResult(Result<string>.Failure(new Error("Charges.DateInvalid")));
            }

            Calls.Add($"pay {date}");
            return Task.FromResult(Result<string>.Success("Pagament registrat."));
        },
        (id, reason, _) => WithReason("exempt", reason),
        (id, reason, _) => WithReason("waive", reason),
        (id, reason, _) => WithReason("void", reason),
        (id, reason, _) => WithReason("revert", reason),
        (id, amount, reason, _) =>
        {
            Calls.Add($"adjust {amount}");
            return Task.FromResult(Result<string>.Success("Import canviat."));
        },
        (student, year, _) =>
        {
            Calls.Add("key");
            return Task.FromResult(Result<string>.Success("Reposició cobrada."));
        },
        () => Today);

    Task<Result<string>> WithReason(string name, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Task.FromResult(Result<string>.Failure(new Error("Charges.ReasonRequired")));
        }

        Calls.Add($"{name} {reason}");
        return Task.FromResult(Result<string>.Success("Fet."));
    }
}

public sealed class ChargesScreenTests
{
    const string Spec = "pantalles-de-domini/pantalles-cobraments";

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
        public string? Answer { get; set; }

        public List<ChoiceRequest> Asked { get; } = [];

        public Task<string?> ChooseAsync(ChoiceRequest request)
        {
            Asked.Add(request);
            return Task.FromResult(Answer);
        }
    }

    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();
    readonly ResxLocalizer _localizer = new();
    readonly FakeForms _forms = new();
    readonly FakeChoices _choices = new();
    readonly FakeChargeWorld _world = new();
    RecordingConfirmations _confirmations = new(true);
    int _courseOpened;
    int _refreshes;

    ScreenContext Context() => new(
        _localizer, _notifications, _log, _delay, _confirmations, _forms, () =>
        {
            _refreshes++;
            return Task.CompletedTask;
        }, _choices);

    StudentChargesViewModel Charges() => new(_world.Services(), Context(), () =>
    {
        _courseOpened++;
        return Task.CompletedTask;
    });

    // --- Charges of a student ---

    [Fact]
    [Trait("spec", Spec + ": Cargos de un alumno (Alumno al corriente, Cargos de cursos anteriores)")]
    public async Task The_charges_of_any_year_are_listed_with_a_summary_of_the_standing()
    {
        _world.Line("Fee", "Paid", 50m);
        _world.Line("Deposit", "Pending", 20m, current: false);
        var model = Charges();

        await model.ShowAsync(Guid.NewGuid());

        Assert.Equal(2, model.Charges.List.Rows.Count);
        Assert.Contains("pendents", model.Summary, StringComparison.Ordinal);
        Assert.Contains("20", model.Summary, StringComparison.Ordinal);
        Assert.Equal("pendent", model.Charges.List.Columns.Single(c => c.Id == "status").Text(model.Charges.List.Rows.Single(r => r.Status == "Pending")));
    }

    [Fact]
    [Trait("spec", Spec + ": Cargos de un alumno (Alumno al corriente, Alumno sin cargos)")]
    public async Task A_student_up_to_date_says_so_and_one_without_charges_explains_when_they_are_generated()
    {
        var model = Charges();
        await model.ShowAsync(Guid.NewGuid());
        Assert.Contains("al corrent", model.Summary, StringComparison.Ordinal);
        Assert.Contains("assignar-li una taquilla", model.Charges.State.Message, StringComparison.Ordinal);

        _world.Line("Fee", "Paid", 50m);
        await model.ShowAsync(Guid.NewGuid());
        Assert.Contains("al corrent", model.Summary, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Operaciones sobre un cargo (Cargo anulado, Los pagos no se editan ni se borran)")]
    public async Task A_pending_charge_offers_its_operations_and_a_voided_one_none_and_none_edits_or_deletes()
    {
        var pending = _world.Line("Fee", "Pending", 50m);
        var voided = _world.Line("Deposit", "Voided", 20m);
        var model = Charges();
        await model.ShowAsync(Guid.NewGuid());

        await model.Detail.ShowAsync(true, pending.Id);
        Assert.Equal(["Pay", "Exempt", "Waive", "Adjust", "Revert", "Void"], model.Detail.Actions.Select(a => a.Id));
        Assert.True(model.Detail.Actions.Single(a => a.Id == "Pay").IsAvailable);
        Assert.False(model.Detail.Actions.Single(a => a.Id == "Revert").IsAvailable);

        await model.Detail.ShowAsync(true, voided.Id);
        Assert.All(model.Detail.Actions, a => Assert.False(a.IsAvailable));
        Assert.DoesNotContain(model.Detail.Actions, a => a.Id is "Edit" or "Delete");
        Assert.NotNull(model.Detail.Actions[0].UnavailableReason);
    }

    // --- Operations ---

    [Fact]
    [Trait("spec", Spec + ": Operaciones sobre un cargo (Pagar, Fecha futura)")]
    public async Task Paying_proposes_today_and_a_future_date_is_marked_and_a_valid_one_registers_once()
    {
        var pending = _world.Line("Fee", "Pending", 50m);
        var model = Charges();
        await model.ShowAsync(Guid.NewGuid());
        await model.Detail.ShowAsync(true, pending.Id);

        model.Detail.Actions.Single(a => a.Id == "Pay").Execute(null);
        await Task.Delay(100);
        var form = (FormViewModel<string>)_forms.Last;
        Assert.Equal(_localizer.Format(_world.Today), form["Date"].Text);

        form["Date"].Text = _localizer.Format(_world.Today.AddDays(3));
        await form.Save.RunAsync();
        Assert.True(form["Date"].HasError);
        Assert.Empty(_world.Calls);

        form["Date"].Text = _localizer.Format(_world.Today);
        var first = form.Save.RunAsync();
        await form.Save.RunAsync(); // the second click while the first runs
        await first;
        Assert.Single(_world.Calls);
        Assert.Single(_notifications.Published);
    }

    [Fact]
    [Trait("spec", Spec + ": Operaciones sobre un cargo (Motivo obligatorio)")]
    public async Task Exempting_or_waiving_without_a_reason_marks_the_reason_field_and_changes_nothing()
    {
        var pending = _world.Line("Fee", "Pending", 50m);
        var model = Charges();
        await model.ShowAsync(Guid.NewGuid());
        await model.Detail.ShowAsync(true, pending.Id);

        foreach (var id in new[] { "Exempt", "Waive" })
        {
            model.Detail.Actions.Single(a => a.Id == id).Execute(null);
            await Task.Delay(100);
            var form = (FormViewModel<string>)_forms.Last;
            Assert.Contains("500", form.Note, StringComparison.Ordinal);
            await form.Save.RunAsync();
            Assert.True(form["Reason"].HasError);
            form["Reason"].Text = "Beca";
            await form.Save.RunAsync();
        }

        Assert.Equal(["exempt Beca", "waive Beca"], _world.Calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Operaciones sobre un cargo (Revertir)")]
    public async Task Reverting_asks_with_the_effect_and_only_reverts_when_confirmed()
    {
        var paid = _world.Line("Fee", "Paid", 50m);
        _confirmations = new RecordingConfirmations(false);
        var model = Charges();
        await model.ShowAsync(Guid.NewGuid());
        await model.Detail.ShowAsync(true, paid.Id);

        model.Detail.Actions.Single(a => a.Id == "Revert").Execute(null);
        await Task.Delay(100);
        var form = (FormViewModel<string>)_forms.Last;
        form["Reason"].Text = "Error de cobrament";
        await form.Save.RunAsync();

        Assert.Contains("pagat", _confirmations.Asked.Single().Consequence, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_world.Calls);
        Assert.Empty(_notifications.Published);

        _confirmations = new RecordingConfirmations(true);
        var again = Charges();
        await again.ShowAsync(Guid.NewGuid());
        await again.Detail.ShowAsync(true, paid.Id);
        again.Detail.Actions.Single(a => a.Id == "Revert").Execute(null);
        await Task.Delay(100);
        var second = (FormViewModel<string>)_forms.Last;
        second["Reason"].Text = "Error de cobrament";
        await second.Save.RunAsync();
        Assert.Equal(["revert Error de cobrament"], _world.Calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Operaciones sobre un cargo (Cambiar importe, anular)")]
    public async Task Voiding_confirms_that_it_is_final_and_an_amount_that_is_not_a_number_marks_its_field()
    {
        var pending = _world.Line("Fee", "Pending", 50m);
        var model = Charges();
        await model.ShowAsync(Guid.NewGuid());
        await model.Detail.ShowAsync(true, pending.Id);

        model.Detail.Actions.Single(a => a.Id == "Adjust").Execute(null);
        await Task.Delay(100);
        var adjust = (FormViewModel<string>)_forms.Last;
        Assert.Equal("50,00", adjust["Amount"].Text);
        adjust["Amount"].Text = "molt";
        await adjust.Save.RunAsync();
        Assert.True(adjust["Amount"].HasError);
        adjust["Amount"].Text = "45,5";
        adjust["Reason"].Text = "Descompte";
        await adjust.Save.RunAsync();
        Assert.Contains("adjust 45,5", _world.Calls);

        model.Detail.Actions.Single(a => a.Id == "Void").Execute(null);
        await Task.Delay(100);
        var voiding = (FormViewModel<string>)_forms.Last;
        voiding["Reason"].Text = "Duplicat";
        await voiding.Save.RunAsync();
        Assert.True(_confirmations.Asked.Single().Destructive);
        Assert.Contains("void Duplicat", _world.Calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Historial del cargo (Historial visible)")]
    public async Task The_history_of_a_charge_loads_only_when_it_is_opened()
    {
        var paid = _world.Line("Fee", "Paid", 50m);
        var model = Charges();
        await model.ShowAsync(Guid.NewGuid());
        await model.Detail.ShowAsync(true, paid.Id);
        Assert.False(model.Detail.HistoryLoaded);

        await model.Detail.LoadHistoryAsync();

        Assert.Equal(["Marcat com a pagat."], model.Detail.History);
    }

    // --- Key replacement ---

    [Fact]
    [Trait("spec", Spec + ": Reposición de llave bajo demanda (Reposición)")]
    public async Task Charging_a_key_replacement_asks_with_the_amount_and_creates_the_charge_only_when_confirmed()
    {
        _confirmations = new RecordingConfirmations(false);
        var model = Charges();
        await model.ShowAsync(Guid.NewGuid());
        await model.ChargeKeyReplacementAsync();
        Assert.Contains("10", _confirmations.Asked.Single().Consequence, StringComparison.Ordinal);
        Assert.Empty(_world.Calls);

        _confirmations = new RecordingConfirmations(true);
        var again = Charges();
        await again.ShowAsync(Guid.NewGuid());
        await again.ChargeKeyReplacementAsync();
        Assert.Equal(["key"], _world.Calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Reposición de llave bajo demanda (Importe sin definir)")]
    public async Task Without_the_replacement_amount_it_says_so_and_offers_to_go_to_the_course_section()
    {
        _world.KeyAmount = null;
        _choices.Answer = "course";
        var model = Charges();
        await model.ShowAsync(Guid.NewGuid());

        await model.ChargeKeyReplacementAsync();

        Assert.Contains("imports", _choices.Asked.Single().Message, StringComparison.Ordinal);
        Assert.Equal(1, _courseOpened);
        Assert.Empty(_world.Calls);
    }

    [Fact]
    [Trait("spec", Spec + ": Reposición de llave bajo demanda")]
    public async Task Without_an_active_year_the_key_replacement_is_disabled_with_the_reason()
    {
        _world.KeyBlocked = new Error("SchoolYears.NoActiveYear");
        var model = Charges();

        await model.ShowAsync(Guid.NewGuid());

        Assert.False(model.KeyReplacement.IsAvailable);
        Assert.Contains("curs actiu", model.KeyReplacement.UnavailableReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("spec", Spec + ": Cargos de un alumno")]
    public async Task A_load_that_arrives_after_the_student_was_cleared_does_not_show_their_charges()
    {
        _world.Line("Fee", "Pending", 50m);
        var model = Charges();
        _world.Gate = new TaskCompletionSource();

        var late = model.ShowAsync(Guid.NewGuid()); // still loading...
        await model.ShowAsync(null); // ...when the person chooses nobody
        _world.Gate.SetResult();
        await late;

        Assert.Null(model.Screen);
        Assert.Empty(model.Charges.List.Rows);
    }
}
