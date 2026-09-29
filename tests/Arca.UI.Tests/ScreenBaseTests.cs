// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Actions;
using Arca.UI.Lists;
using Arca.UI.Screens;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Arca.UI.Tests;

public sealed class ScreenBaseTests
{
    const string Spec = "pantalles-de-domini/design";

    sealed record Row(Guid Id, string Name);

    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();
    readonly ResxLocalizer _localizer = new();
    readonly Error _datesInvalid = new("SchoolYears.DatesInvalid");
    readonly Error _formWide = new("SchoolYears.Overlaps");

    ScreenListViewModel<Row, Guid> List(Func<Task<Result<IReadOnlyList<Row>>>> query) => new(
        [new ListColumn<Row>("name", "Nom", r => r.Name)], r => r.Id, _ => query(), _localizer, _notifications, _log);

    static Result<IReadOnlyList<Row>> Rows(params Row[] rows) => Result<IReadOnlyList<Row>>.Success(rows);

    // --- D1: list and detail ---

    [Fact]
    [Trait("spec", Spec + ": D1 Una pantalla = un modelo de vista de lista y otro de detalle")]
    public async Task A_list_says_empty_when_there_is_nothing_and_no_results_when_a_filter_hides_everything()
    {
        var data = Rows();
        var list = List(() => Task.FromResult(data));

        await list.LoadAsync();
        Assert.Equal(ListViewState.Empty, list.State.State);

        data = Rows(new Row(Guid.NewGuid(), "Berta"));
        await list.LoadAsync();
        Assert.Equal(ListViewState.Content, list.State.State);

        list.List.FilterText = "zzz";
        Assert.Equal(ListViewState.NoResults, list.State.State);
        list.State.Actions[0].Command.Execute(null);
        Assert.Equal(ListViewState.Content, list.State.State);
    }

    [Fact]
    [Trait("spec", Spec + ": D1 Una pantalla = un modelo de vista de lista y otro de detalle")]
    public async Task The_selection_survives_loading_again_by_identity_and_is_dropped_when_the_row_is_gone()
    {
        var berta = new Row(Guid.NewGuid(), "Berta");
        var zoe = new Row(Guid.NewGuid(), "Zoe");
        var data = Rows(zoe, berta);
        var list = List(() => Task.FromResult(data));
        await list.LoadAsync();
        list.Select(berta);

        data = Rows(berta with { Name = "Berta B." }, zoe);
        await list.LoadAsync();
        Assert.Equal("Berta B.", list.Current!.Name);

        data = Rows(zoe);
        await list.LoadAsync();
        Assert.Null(list.Current);
        Assert.False(list.TryGetSelectedKey(out _));
    }

    [Fact]
    [Trait("spec", Spec + ": D1 Una pantalla = un modelo de vista de lista y otro de detalle")]
    public async Task A_slow_older_answer_never_overwrites_a_newer_one()
    {
        var slow = new TaskCompletionSource<Result<IReadOnlyList<Row>>>();
        var calls = 0;
        var list = List(() => ++calls == 1 ? slow.Task : Task.FromResult(Rows(new Row(Guid.NewGuid(), "Nova"))));

        var first = list.LoadAsync();
        await list.LoadAsync();
        slow.SetResult(Rows(new Row(Guid.NewGuid(), "Vella")));
        await first;

        Assert.Equal(["Nova"], list.List.Rows.Select(r => r.Name));
    }

    [Fact]
    [Trait("spec", Spec + ": D1 Una pantalla = un modelo de vista de lista y otro de detalle")]
    public async Task A_failing_query_is_explained_and_does_not_leave_the_list_loading()
    {
        var list = List(() => Task.FromResult(Result<IReadOnlyList<Row>>.Failure(_formWide)));

        await list.LoadAsync();

        Assert.NotEqual(ListViewState.Loading, list.State.State);
        Assert.Single(_notifications.Published);
    }

    [Fact]
    [Trait("spec", Spec + ": D4 Lista con detalle en el mismo panel")]
    public async Task The_detail_loads_the_history_only_when_it_is_asked_for_and_then_follows_the_element()
    {
        var historyCalls = 0;
        var detail = new DetailViewModel<Guid, Row>(
            (id, _) => Task.FromResult(Result<Row?>.Success(new Row(id, "Berta"))),
            row => [new AppAction("Edit", "Edita")], _localizer, _notifications, _log,
            (_, _) =>
            {
                historyCalls++;
                return Task.FromResult(Result<IReadOnlyList<string>>.Success(["alta"]));
            });

        await detail.ShowAsync(true, Guid.NewGuid());
        Assert.Equal(0, historyCalls);
        Assert.Single(detail.Actions);

        await detail.LoadHistoryAsync();
        await detail.ShowAsync(true, Guid.NewGuid());
        Assert.Equal(2, historyCalls);

        await detail.ShowAsync(false, Guid.Empty);
        Assert.Null(detail.Detail);
        Assert.Empty(detail.Actions);
        Assert.False(detail.HistoryLoaded);
    }

    // --- D2: actions ---

    [Fact]
    [Trait("spec", Spec + ": D2 Acciones como objetos compartidos")]
    public void An_action_is_disabled_with_the_reason_application_gave_and_enabled_again_when_it_no_longer_applies()
    {
        Error? refusal = _datesInvalid;
        var ran = 0;
        var actions = new ActionSet(_localizer);
        var action = actions.Add("Activate", "Common.Action.Save", () => ran++, () => refusal);

        Assert.False(action.IsAvailable);
        Assert.Equal(_localizer.Message(_datesInvalid), action.ToolTipText);
        action.Execute(null);
        Assert.Equal(0, ran);

        refusal = null;
        actions.Refresh();
        Assert.True(action.IsAvailable);
        action.Execute(null);
        Assert.Equal(1, ran);
    }

    // --- D3: forms ---

    FormViewModel<int> Form(Func<Task<Result<int>>> save, Action<int>? saved = null) => new(
        [new FormFieldModel("start", "Inici"), new FormFieldModel("end", "Fi")], _ => save(),
        error => error.Code == "SchoolYears.DatesInvalid" ? "end" : null,
        n => $"{n} desat", "SaveYear", _notifications, _localizer, _log, _delay, "Curs", "Desa", saved);

    [Fact]
    [Trait("spec", Spec + ": D3 Formularios: validar al guardar, sin perder lo escrito")]
    public async Task An_error_of_a_field_shows_next_to_it_keeps_what_was_written_and_goes_away_when_writing_again()
    {
        var form = Form(() => Task.FromResult(Result<int>.Failure(_datesInvalid)));
        form["start"].Text = "2026-09-01";
        form["end"].Text = "2026-01-01";

        await form.Save.RunAsync();

        Assert.True(form["end"].HasError);
        Assert.False(form["start"].HasError);
        Assert.Equal("2026-01-01", form["end"].Text);
        Assert.NotNull(form.Summary);
        Assert.Empty(_notifications.Published); // the field explains it; no second message

        form["end"].Text = "2027-06-30";
        Assert.False(form["end"].HasError);
    }

    [Fact]
    [Trait("spec", Spec + ": D3 Formularios: validar al guardar, sin perder lo escrito")]
    public async Task An_error_of_the_whole_form_is_a_notification_and_success_reports_and_runs_the_callback()
    {
        var outcome = Result<int>.Failure(_formWide);
        int? saved = null;
        var form = Form(() => Task.FromResult(outcome), n => saved = n);

        await form.Save.RunAsync();
        Assert.Equal(NotificationKind(), _notifications.Published.Single().Kind);
        Assert.Null(form.Summary);

        outcome = Result<int>.Success(3);
        await form.Save.RunAsync();
        Assert.Equal(3, saved);
        Assert.Equal("3 desat", _notifications.Published.Last().Text);
    }

    static Arca.Application.Feedback.NotificationKind NotificationKind() => Arca.Application.Feedback.NotificationKind.Error;

    [Fact]
    [Trait("spec", Spec + ": D3 Formularios: validar al guardar, sin perder lo escrito")]
    public async Task Saving_twice_at_once_saves_once()
    {
        var gate = new TaskCompletionSource<Result<int>>();
        var calls = 0;
        var form = Form(() =>
        {
            calls++;
            return gate.Task;
        });

        var first = form.Save.RunAsync();
        await form.Save.RunAsync(); // the second click while the first runs
        gate.SetResult(Result<int>.Success(1));
        await first;

        Assert.Equal(1, calls);
    }

    // --- D7: sections ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": D7 Reparto en el registro de secciones")]
    public void A_section_holds_its_screens_as_tabs_and_refuses_a_screen_of_another_section()
    {
        var host = new SectionScreens("Lockers", [new("Lockers", () => new TextBlock { Text = "a" }), new("Zones", () => new TextBlock { Text = "b" })], _localizer);
        Assert.Equal("Lockers", host.CurrentScreenId);

        host.Tabs["Zones"].Command?.Execute(null);
        host.Open("Zones");
        Assert.Equal("Zones", host.CurrentScreenId);

        Assert.Throws<ArgumentException>(() => new SectionScreens("Lockers", [new("Students", () => new TextBlock())], _localizer));
    }
}
