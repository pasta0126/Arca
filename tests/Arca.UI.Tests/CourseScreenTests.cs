// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.ConceptAmounts;
using Arca.Application.ConceptAmounts.GetConceptAmountsHistory;
using Arca.Application.ConceptAmounts.SetConceptAmounts;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Application.SchoolYears;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.SchoolYears.GetYearScreen;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Actions;
using Arca.UI.Course;
using Arca.UI.Lists;
using Arca.UI.Screens;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace Arca.UI.Tests;

public sealed class CourseScreenTests
{
    const string Spec = "pantalles-de-domini/pantalles-curs-i-imports";

    sealed class FakeForms : IFormDialogs
    {
        public IFormModel? Last { get; private set; }

        public Task<bool> ShowAsync(IFormModel form)
        {
            Last = form;
            return Task.FromResult(false);
        }
    }

    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();
    readonly ResxLocalizer _localizer = new();
    readonly FakeForms _forms = new();
    readonly List<AcademicYearSummary> _years = [];
    readonly Dictionary<Guid, (decimal Fee, decimal Deposit, decimal Key)> _amounts = [];
    readonly HashSet<Guid> _withCharges = [];
    readonly HashSet<Guid> _withData = [];
    readonly List<SetConceptAmountsRequest> _saved = [];
    RecordingConfirmations _confirmations = new(true);
    int _refreshes;

    AcademicYearSummary AddYear(int start, bool active = false)
    {
        var year = new AcademicYearSummary(Guid.NewGuid(), $"{start}-{start + 1}", new DateOnly(start, 9, 1), new DateOnly(start + 1, 6, 30), active);
        _years.Add(year);
        return year;
    }

    CourseServices Services() => new(
        _ => Task.FromResult(Result<IReadOnlyList<AcademicYearSummary>>.Success([.. _years.OrderByDescending(y => y.StartDate)])),
        (id, _) =>
        {
            var year = _years.Single(y => y.Id == id);
            return Task.FromResult(Result<YearScreenDetail>.Success(new YearScreenDetail(
                year,
                year.IsActive || !_years.Any(y => y.IsActive) ? null : new Error("SchoolYears.AnotherActive"),
                _withData.Contains(id) ? new Error("SchoolYears.HasData") : null,
                _withCharges.Contains(id), _amounts.ContainsKey(id),
                year.EndDate < new DateOnly(2026, 9, 29) ? new Error("ConceptAmounts.YearFinished") : null,
                !year.IsActive && year.EndDate < new DateOnly(2026, 9, 29))));
        },
        (request, _) =>
        {
            var created = new AcademicYearSummary(Guid.NewGuid(), $"{request.StartDate.Year}-{request.StartDate.Year + 1}", request.StartDate, request.EndDate, _years.Count == 0);
            if (request.EndDate <= request.StartDate)
            {
                return Task.FromResult(Result<AcademicYearSummary>.Failure(new Error("SchoolYears.DatesInvalid")));
            }

            _years.Add(created);
            return Task.FromResult(Result<AcademicYearSummary>.Success(created));
        },
        (id, _) =>
        {
            var index = _years.FindIndex(y => y.Id == id);
            _years[index] = _years[index] with { IsActive = true };
            return Task.FromResult(Result<AcademicYearSummary>.Success(_years[index]));
        },
        (id, _) =>
        {
            _years.RemoveAll(y => y.Id == id);
            return Task.FromResult(Result<Guid>.Success(id));
        },
        (id, _) =>
        {
            var year = _years.Single(y => y.Id == id);
            if (_amounts.TryGetValue(id, out var own))
            {
                return Task.FromResult(Result<ConceptAmountsView>.Success(new ConceptAmountsView(id, own.Fee, own.Deposit, own.Key, true, false)));
            }

            var previous = _years.Where(y => y.StartDate < year.StartDate && _amounts.ContainsKey(y.Id)).OrderByDescending(y => y.StartDate).FirstOrDefault();
            return Task.FromResult(Result<ConceptAmountsView>.Success(previous is null
                ? new ConceptAmountsView(id, null, null, null, true, false)
                : new ConceptAmountsView(id, _amounts[previous.Id].Fee, _amounts[previous.Id].Deposit, _amounts[previous.Id].Key, true, true, previous.Name)));
        },
        (request, _) =>
        {
            if (request.Fee > 9999.99m)
            {
                return Task.FromResult(Result<ConceptAmountsView>.Failure(new Error("ConceptAmounts.AmountInvalid", Args: [9999.99m, "Fee"])));
            }

            _saved.Add(request);
            _amounts[request.YearId] = (request.Fee, request.Deposit, request.KeyReplacementFee);
            return Task.FromResult(Result<ConceptAmountsView>.Success(new ConceptAmountsView(request.YearId, request.Fee, request.Deposit, request.KeyReplacementFee, true, false)));
        },
        (_, _) => Task.FromResult(Result<IReadOnlyList<AmountHistoryLine>>.Success([new AmountHistoryLine(DateTimeOffset.UtcNow, "Quota: definit a 50,00 €.")])));

    CourseViewModel Model()
    {
        var registry = new ActionRegistry(_localizer, UiPlatform.Windows);
        return new CourseViewModel(
            Services(),
            new ScreenContext(_localizer, _notifications, _log, _delay, _confirmations, _forms, () =>
            {
                _refreshes++;
                return Task.CompletedTask;
            }),
            registry[StandardActions.New]);
    }

    // --- Lista de cursos ---

    [Fact]
    [Trait("spec", Spec + ": Lista de cursos (Lista con curso activo)")]
    public async Task The_years_come_most_recent_first_with_the_state_told_in_words()
    {
        AddYear(2025);
        AddYear(2026, active: true);
        var model = Model();

        await model.LoadAsync();

        Assert.Equal(["2026-2027", "2025-2026"], model.Years.List.Rows.Select(y => y.Name));
        Assert.Equal("Actiu", model.StateOf(model.Years.List.Rows[0]));
        Assert.Equal("Inactiu", model.StateOf(model.Years.List.Rows[1]));
    }

    [Fact]
    [Trait("spec", Spec + ": Lista de cursos (Sin cursos)")]
    public async Task Without_years_the_list_explains_that_one_is_needed_and_offers_to_create_the_first()
    {
        var model = Model();

        await model.LoadAsync();

        Assert.Equal(ListViewState.Empty, model.Years.State.State);
        Assert.Contains("curs escolar", model.Years.State.Message, StringComparison.Ordinal);
        Assert.Equal(model.NewYear.Label, model.Years.State.Actions.Single().Label);
    }

    // --- Crear ---

    [Fact]
    [Trait("spec", Spec + ": Crear un curso desde un formulario (Nombre derivado)")]
    public async Task Typing_the_start_year_shows_the_derived_name_and_proposes_the_dates()
    {
        var model = Model();
        await model.NewYearAsync();
        var form = _forms.Last!;

        form.Fields.Single(f => f.Id == "StartYear").Text = "2026";

        Assert.Contains("2026-2027", form.Note, StringComparison.Ordinal);
        Assert.Equal(_localizer.Format(new DateOnly(2026, 9, 1)), form.Fields.Single(f => f.Id == "StartDate").Text);
        Assert.Equal(_localizer.Format(new DateOnly(2027, 6, 30)), form.Fields.Single(f => f.Id == "EndDate").Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Crear un curso desde un formulario (Fechas no válidas)")]
    public async Task An_end_before_the_start_marks_the_field_and_keeps_what_was_written()
    {
        var model = Model();
        await model.NewYearAsync();
        var form = (FormViewModel<AcademicYearSummary>)_forms.Last!;
        form.Fields.Single(f => f.Id == "StartDate").Text = _localizer.Format(new DateOnly(2026, 9, 1));
        form.Fields.Single(f => f.Id == "EndDate").Text = _localizer.Format(new DateOnly(2026, 1, 1));

        await form.Save.RunAsync();

        Assert.True(form.Fields.Single(f => f.Id == "StartDate").HasError);
        Assert.Equal(_localizer.Format(new DateOnly(2026, 1, 1)), form.Fields.Single(f => f.Id == "EndDate").Text);
        Assert.Empty(_years);
    }

    [Fact]
    [Trait("spec", Spec + ": Crear un curso desde un formulario (Fechas no válidas)")]
    public async Task A_text_that_is_not_a_date_marks_that_field()
    {
        var model = Model();
        await model.NewYearAsync();
        var form = (FormViewModel<AcademicYearSummary>)_forms.Last!;
        form.Fields.Single(f => f.Id == "StartDate").Text = _localizer.Format(new DateOnly(2026, 9, 1));
        form.Fields.Single(f => f.Id == "EndDate").Text = "demà";

        await form.Save.RunAsync();

        Assert.True(form.Fields.Single(f => f.Id == "EndDate").HasError);
        Assert.False(form.Fields.Single(f => f.Id == "StartDate").HasError);
    }

    [Fact]
    [Trait("spec", Spec + ": Crear un curso desde un formulario (Curso creado)")]
    public async Task A_valid_year_is_created_notified_once_listed_and_chosen()
    {
        var model = Model();
        await model.NewYearAsync();
        var form = (FormViewModel<AcademicYearSummary>)_forms.Last!;
        form.Fields.Single(f => f.Id == "StartYear").Text = "2026";

        await form.Save.RunAsync();

        Assert.Equal("Curs 2026-2027 creat.", _notifications.Published.Single().Text);
        Assert.Equal("2026-2027", model.Years.Current!.Name);
        Assert.Equal(1, _refreshes);
    }

    // --- Activar y eliminar ---

    [Fact]
    [Trait("spec", Spec + ": Activar un curso (Ya hay un curso activo)")]
    public async Task Activate_is_disabled_with_the_reason_while_another_year_is_active()
    {
        AddYear(2026, active: true);
        var other = AddYear(2027);
        var model = Model();
        await model.LoadAsync();

        model.Years.Select(model.Years.List.Rows.Single(y => y.Id == other.Id));
        await Task.Delay(50);
        await model.Detail.ShowAsync(true, other.Id);

        var activate = model.Detail.Actions.Single(a => a.Id == "Activate");
        Assert.False(activate.IsAvailable);
        Assert.Equal(_localizer.Message(new Error("SchoolYears.AnotherActive")), activate.UnavailableReason);
    }

    [Fact]
    [Trait("spec", Spec + ": Activar un curso (Activar sin curso activo)")]
    public async Task Activating_asks_what_changes_and_only_then_activates()
    {
        var year = AddYear(2026);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, year.Id);

        model.Detail.Actions.Single(a => a.Id == "Activate").Execute(null);
        await Task.Delay(100);

        var asked = Assert.Single(_confirmations.Asked);
        Assert.Contains("2026-2027", asked.Consequence, StringComparison.Ordinal);
        Assert.True(_years.Single().IsActive);
        Assert.Contains(_notifications.Published, n => n.Text == "Curs 2026-2027 activat.");
    }

    [Fact]
    [Trait("spec", Spec + ": Activar un curso (Activar sin curso activo)")]
    public async Task Declining_the_confirmation_leaves_the_year_as_it_was()
    {
        _confirmations = new RecordingConfirmations(false);
        var year = AddYear(2026);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, year.Id);

        model.Detail.Actions.Single(a => a.Id == "Activate").Execute(null);
        await Task.Delay(100);

        Assert.False(_years.Single().IsActive);
        Assert.Empty(_notifications.Published);
    }

    [Fact]
    [Trait("spec", Spec + ": Eliminar un curso vacío (Curso con datos)")]
    public async Task A_year_with_data_cannot_be_deleted_and_says_why_and_an_empty_one_is_deleted_after_confirming()
    {
        var withData = AddYear(2025);
        var empty = AddYear(2027);
        _withData.Add(withData.Id);
        var model = Model();
        await model.LoadAsync();

        await model.Detail.ShowAsync(true, withData.Id);
        var blocked = model.Detail.Actions.Single(a => a.Id == "Delete");
        Assert.False(blocked.IsAvailable);
        Assert.NotNull(blocked.UnavailableReason);

        await model.Detail.ShowAsync(true, empty.Id);
        model.Detail.Actions.Single(a => a.Id == "Delete").Execute(null);
        await Task.Delay(100);

        Assert.True(_confirmations.Asked.Single().Destructive);
        Assert.DoesNotContain(_years, y => y.Id == empty.Id);
        Assert.Null(model.Detail.Detail);
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle del curso de solo lectura si no está activo (Curso cerrado)")]
    public async Task A_finished_inactive_year_is_shown_as_history_and_its_amounts_cannot_be_changed()
    {
        var old = AddYear(2020);
        AddYear(2026, active: true);
        var model = Model();
        await model.LoadAsync();

        await model.Detail.ShowAsync(true, old.Id);

        Assert.True(model.Detail.Detail!.Year.IsHistoric);
        Assert.False(model.Detail.Actions.Single(a => a.Id == "DefineAmounts").IsAvailable);
    }

    // --- Importes ---

    [Fact]
    [Trait("spec", Spec + ": Importes del curso (Propuesta heredada)")]
    public async Task A_year_without_amounts_is_proposed_the_previous_ones_and_nothing_is_saved_until_confirmed()
    {
        var previous = AddYear(2025);
        var year = AddYear(2026, active: true);
        _amounts[previous.Id] = (50m, 20m, 10m);
        var model = Model();

        await model.DefineAmountsAsync(year.Id);

        var form = _forms.Last!;
        Assert.Equal(["50,00", "20,00", "10,00"], form.Fields.Select(f => f.Text));
        Assert.Contains("2025-2026", form.Note, StringComparison.Ordinal);
        Assert.Empty(_saved);
    }

    [Fact]
    [Trait("spec", Spec + ": Importes del curso (Primer curso)")]
    public async Task With_no_previous_year_the_fields_are_empty_and_a_note_gives_the_valid_range()
    {
        var year = AddYear(2026, active: true);
        var model = Model();

        await model.DefineAmountsAsync(year.Id);

        Assert.All(_forms.Last!.Fields, f => Assert.Equal(string.Empty, f.Text));
        Assert.Contains("9.999,99", _forms.Last.Note, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Importes del curso (Coma decimal)")]
    public async Task A_decimal_comma_is_accepted_and_saved_as_that_amount()
    {
        var year = AddYear(2026, active: true);
        var model = Model();
        await model.DefineAmountsAsync(year.Id);
        var form = (FormViewModel<ConceptAmountsView>)_forms.Last!;
        form.Fields[0].Text = "50,5";
        form.Fields[1].Text = "20";
        form.Fields[2].Text = "10,25";

        await form.Save.RunAsync();

        var saved = Assert.Single(_saved);
        Assert.Equal((50.5m, 20m, 10.25m), (saved.Fee, saved.Deposit, saved.KeyReplacementFee));
        Assert.Equal("Imports del curs 2026-2027 desats.", _notifications.Published.Single().Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Importes del curso (Importe no válido)")]
    public async Task A_value_that_is_not_a_number_and_a_value_out_of_range_each_mark_their_own_field()
    {
        var year = AddYear(2026, active: true);
        var model = Model();
        await model.DefineAmountsAsync(year.Id);
        var form = (FormViewModel<ConceptAmountsView>)_forms.Last!;
        form.Fields[0].Text = "50";
        form.Fields[1].Text = "vint";
        form.Fields[2].Text = "10";

        await form.Save.RunAsync();
        Assert.True(form.Fields[1].HasError);
        Assert.False(form.Fields[0].HasError);

        form.Fields[1].Text = "20";
        form.Fields[0].Text = "10000";
        await form.Save.RunAsync();
        Assert.True(form.Fields[0].HasError);
        Assert.Equal("10000", form.Fields[0].Text);
        Assert.Empty(_saved);
    }

    [Fact]
    [Trait("spec", Spec + ": Aviso del efecto de cambiar un importe (Cambio con cargos existentes)")]
    public async Task Changing_amounts_of_a_year_with_charges_warns_first_and_saves_only_when_confirmed()
    {
        var year = AddYear(2026, active: true);
        _amounts[year.Id] = (50m, 20m, 10m);
        _withCharges.Add(year.Id);
        _confirmations = new RecordingConfirmations(false);
        var model = Model();
        await model.DefineAmountsAsync(year.Id);
        var form = (FormViewModel<ConceptAmountsView>)_forms.Last!;
        form.Fields[0].Text = "60";

        await form.Save.RunAsync();

        Assert.Contains("càrrecs que es generin", _confirmations.Asked.Single().Consequence, StringComparison.Ordinal);
        Assert.Empty(_saved);
        Assert.Empty(_notifications.Published); // declining is not an error

        _confirmations = new RecordingConfirmations(true);
        var again = Model();
        await again.DefineAmountsAsync(year.Id);
        var second = (FormViewModel<ConceptAmountsView>)_forms.Last!;
        second.Fields[0].Text = "60";
        await second.Save.RunAsync();
        Assert.Single(_saved);
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback y doble ejecución en la sección Curso (Doble clic en guardar)")]
    public async Task Saving_the_amounts_twice_at_once_saves_once_and_notifies_once()
    {
        var year = AddYear(2026, active: true);
        var model = Model();
        await model.DefineAmountsAsync(year.Id);
        var form = (FormViewModel<ConceptAmountsView>)_forms.Last!;
        form.Fields[0].Text = "50";
        form.Fields[1].Text = "20";
        form.Fields[2].Text = "10";

        var first = form.Save.RunAsync();
        await form.Save.RunAsync();
        await first;

        Assert.Single(_saved);
        Assert.Single(_notifications.Published);
    }

    [Fact]
    [Trait("spec", Spec + ": Curso sin importes visible (Curso activo sin importes)")]
    public async Task The_active_year_without_amounts_shows_a_notice_with_a_way_to_define_them()
    {
        AddYear(2026, active: true);
        var model = Model();

        await model.LoadAsync();

        Assert.True(model.HasMissingAmountsNotice);
        Assert.Contains("2026-2027", model.MissingAmountsNotice, StringComparison.Ordinal);
        model.DefineActiveAmounts.Execute(null);
        await Task.Delay(50);
        Assert.NotNull(_forms.Last);
    }

    [Fact]
    [Trait("spec", Spec + ": Importes del curso (Aviso del efecto: Historial de importes)")]
    public async Task The_history_of_the_amounts_loads_only_when_it_is_opened()
    {
        var year = AddYear(2026, active: true);
        _amounts[year.Id] = (50m, 20m, 10m);
        var model = Model();
        await model.LoadAsync();
        await model.Detail.ShowAsync(true, year.Id);

        Assert.False(model.Detail.HistoryLoaded);
        await model.Detail.LoadHistoryAsync();

        Assert.Contains("definit a 50,00", model.Detail.History.Single(), StringComparison.Ordinal);
    }
    // --- The views ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Lista de cursos (Lista con curso activo)")]
    public async Task The_section_draws_its_action_the_rows_and_the_detail_of_the_row_chosen()
    {
        AddYear(2025);
        AddYear(2026, active: true);
        var model = Model();
        var screen = CourseView.Create(model, _localizer);
        var window = new Window { Content = screen, Width = 1000, Height = 600 };
        window.Show();
        await model.LoadAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Nou curs", Assert.Single(screen.Buttons).Content);
        Assert.Equal(2, model.Years.List.Rows.Count);

        model.Years.Select(model.Years.List.Rows[0]);
        await Task.Delay(100);
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(model.Detail.Detail);
        window.Close();
    }

    [AvaloniaFact]
    [Trait("spec", "pantalles-de-domini/design: D3 Formularios: validar al guardar, sin perder lo escrito")]
    public async Task The_form_window_shows_the_error_next_to_its_field_and_stays_open()
    {
        var model = Model();
        await model.NewYearAsync();
        var form = (FormViewModel<AcademicYearSummary>)_forms.Last!;
        var window = new FormDialogWindow(form, _localizer);
        window.Show();
        window.Boxes["StartDate"].Text = _localizer.Format(new DateOnly(2026, 9, 1));
        window.Boxes["EndDate"].Text = _localizer.Format(new DateOnly(2026, 1, 1));

        await form.Save.RunAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.IsVisible);
        Assert.True(form.Fields.Single(f => f.Id == "StartDate").HasError);
        Assert.Equal(_localizer.Format(new DateOnly(2026, 1, 1)), window.Boxes["EndDate"].Text);
        window.Close();
    }
}
