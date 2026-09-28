// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Feedback;
using Arca.Application.Localization;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.UI.Assigning;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Xunit;

namespace Arca.UI.Tests;

public sealed class AssignmentDragTests
{
    const string Spec = "ux-fonaments/arrossegar-i-deixar-anar";

    static readonly Guid _student = Guid.NewGuid();
    static readonly Guid _freeLocker = Guid.NewGuid();
    static readonly Guid _takenLocker = Guid.NewGuid();

    readonly ResxLocalizer _localizer = new();
    readonly RecordingNotifications _notifications = new();
    readonly RecordingErrorLog _log = new();
    readonly ManualDelay _delay = new();
    readonly List<AssignLockerRequest> _requests = [];
    TaskCompletionSource? _gate;
    IReadOnlyList<Notice> _warnings = [];

    static AssignmentRow Row() => new(
        Guid.NewGuid(), _student, "Marta Puig", _freeLocker, 7, "Planta 1", "2026-2027", DateTimeOffset.UtcNow, null, null, null);

    async Task<Result<AssignLockerResult>> Assign(AssignLockerRequest request, CancellationToken ct)
    {
        _requests.Add(request);
        if (_gate is not null)
        {
            await _gate.Task;
        }

        return _warnings.Count > 0 && !request.ConfirmWarnings
            ? Result<AssignLockerResult>.Success(new AssignLockerResult(null, _warnings))
            : Result<AssignLockerResult>.Success(new AssignLockerResult(Row(), []));
    }

    static Task<Result<AssignmentTargetCheck>> Check(Guid student, Guid locker, CancellationToken ct) =>
        Task.FromResult(Result<AssignmentTargetCheck>.Success(
            locker == _takenLocker
                ? new AssignmentTargetCheck(Arca.Domain.Lockers.LockerErrors.NotFound, [])
                : new AssignmentTargetCheck(null, [])));

    AssignLockerInteraction Interaction(bool confirm = true, RecordingConfirmations? confirmations = null) =>
        new(Assign, confirmations ?? new RecordingConfirmations(confirm), _localizer, _notifications, _log, _delay);

    AssignmentDropViewModel Model(AssignLockerInteraction interaction, Func<Guid, Guid, CancellationToken, Task<Result<AssignmentTargetCheck>>>? check = null) =>
        new(check ?? Check, interaction, _localizer);

    // --- What is carried ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Solo este caso en v1 (Otras acciones)")]
    public void Only_a_student_of_this_application_can_be_dropped_on_a_locker()
    {
        var student = Guid.NewGuid();
        Assert.Equal(student, StudentDragPayload.TryRead(StudentDragPayload.Create(student)));

        var other = new DataTransfer();
        other.Add(DataTransferItem.CreateText("una zona"));
        Assert.Null(StudentDragPayload.TryRead(other)); // text, a zone, a locker or a file from outside: nothing to assign
        Assert.Null(StudentDragPayload.TryRead(new DataTransfer()));
    }

    // --- Visual answer of the destination ---

    [Fact]
    [Trait("spec", Spec + ": Respuesta visual del destino (Destino válido)")]
    public async Task A_free_locker_lights_up_as_a_valid_destination()
    {
        var model = Model(Interaction());
        model.BeginDrag(_student);

        var feedback = await model.PreviewAsync(_freeLocker);

        Assert.Equal(new DropTargetFeedback(DropTargetState.Valid, null, false), feedback);
        Assert.Equal(feedback, model.Known(_freeLocker)); // a drag-over can answer at once afterwards
        Assert.Empty(_requests); // asking writes nothing
    }

    [Fact]
    [Trait("spec", Spec + ": Respuesta visual del destino (Destino no válido)")]
    public async Task A_locker_that_cannot_receive_the_student_is_marked_invalid_with_the_reason_and_cannot_be_dropped_on()
    {
        var model = Model(Interaction());
        model.BeginDrag(_student);

        var feedback = await model.PreviewAsync(_takenLocker);
        var dropped = await model.DropAsync(_takenLocker);

        Assert.Equal(DropTargetState.Invalid, feedback!.State);
        Assert.Equal("No s'ha trobat la taquilla. Torna a carregar la llista.", feedback.Reason);
        Assert.False(dropped);
        Assert.Empty(_requests);
    }

    [Fact]
    [Trait("spec", Spec + ": Respuesta visual del destino (Destino válido)")]
    public async Task A_valid_destination_that_will_ask_for_confirmation_says_so()
    {
        var model = Model(Interaction(), (_, _, _) => Task.FromResult(Result<AssignmentTargetCheck>.Success(
            new AssignmentTargetCheck(null, [new Notice("Charges.PriorDebt", [2, 70m])]))));
        model.BeginDrag(_student);

        var feedback = await model.PreviewAsync(_freeLocker);

        Assert.True(feedback!.NeedsConfirmation);
        Assert.Equal(DropTargetState.Valid, feedback.State);
    }

    [Fact]
    [Trait("spec", Spec + ": Respuesta visual del destino (Soltar fuera)")]
    public async Task Nothing_is_asked_or_done_when_no_student_is_being_dragged()
    {
        var model = Model(Interaction());

        Assert.Null(await model.PreviewAsync(_freeLocker));
        Assert.False(await model.DropAsync(_freeLocker));
        Assert.Empty(_requests);
    }

    // --- Dropping ---

    [Fact]
    [Trait("spec", Spec + ": Asignar arrastrando un alumno (Asignación arrastrando)")]
    public async Task Dropping_on_a_valid_locker_runs_the_same_request_the_menu_and_the_keyboard_send()
    {
        var interaction = Interaction();
        var model = Model(interaction);
        model.BeginDrag(_student);

        Assert.True(await model.DropAsync(_freeLocker));
        var dragged = Assert.Single(_requests);
        await interaction.AssignAsync(new AssignmentIntent(_student, _freeLocker)); // what the menu or a key does
        var chosen = _requests[1];

        Assert.Equal(dragged, chosen);
        Assert.Equal((_student, _freeLocker, false), (dragged.StudentId, dragged.LockerId, dragged.ConfirmWarnings));
        Assert.False(model.IsDragging);
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback tras soltar (Asignación realizada)")]
    public async Task The_result_says_which_student_has_which_locker()
    {
        var interaction = Interaction();
        var model = Model(interaction);
        model.BeginDrag(_student);

        await model.DropAsync(_freeLocker);

        var only = Assert.Single(_notifications.Published);
        Assert.Equal(NotificationKind.Success, only.Kind);
        Assert.Equal("Marta Puig té ara la taquilla 7 a la zona Planta 1.", only.Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Cancelar el arrastre (Escape durante el arrastre)")]
    public async Task Cancelling_the_drag_changes_nothing_and_a_late_drop_does_nothing()
    {
        var model = Model(Interaction());
        model.BeginDrag(_student);
        await model.PreviewAsync(_freeLocker);

        model.End(); // Escape, or dropped outside any locker

        Assert.False(model.IsDragging);
        Assert.Null(model.Known(_freeLocker));
        Assert.False(await model.DropAsync(_freeLocker));
        Assert.Empty(_requests);
        Assert.Empty(_notifications.Published);
    }

    [Fact]
    [Trait("spec", Spec + ": Feedback tras soltar (Soltar dos veces)")]
    public async Task Dropping_twice_on_the_same_locker_assigns_once()
    {
        _gate = new TaskCompletionSource();
        var interaction = Interaction();
        var model = Model(interaction);
        model.BeginDrag(_student);

        var first = model.DropAsync(_freeLocker);
        var second = await model.DropAsync(_freeLocker);
        Assert.False(second);
        await interaction.AssignAsync(new AssignmentIntent(_student, _freeLocker)); // a click at the same time is ignored too
        _gate.SetResult();
        await first;

        Assert.Single(_requests);
        Assert.Single(_notifications.Published);
    }

    // --- Warnings ---

    [Fact]
    [Trait("spec", Spec + ": Asignar arrastrando un alumno (Aviso con confirmación)")]
    public async Task A_warning_asks_for_the_same_confirmation_and_assigns_only_after_it()
    {
        _warnings = [new Notice("Charges.PriorDebt", [2, 70m])];
        var confirmations = new RecordingConfirmations(true);
        var interaction = Interaction(confirmations: confirmations);
        var model = Model(interaction);
        model.BeginDrag(_student);

        await model.DropAsync(_freeLocker);

        var asked = Assert.Single(confirmations.Asked);
        Assert.Equal("Assignar la taquilla?", asked.Title);
        Assert.Contains("Càrrecs pendents de cursos anteriors: 2", Assert.Single(asked.Details!));
        Assert.Equal([false, true], _requests.Select(r => r.ConfirmWarnings));
        Assert.Equal(NotificationKind.Success, Assert.Single(_notifications.Published).Kind);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignar arrastrando un alumno (Aviso con confirmación)")]
    public async Task Declining_the_confirmation_assigns_nothing_and_says_so()
    {
        _warnings = [new Notice("Charges.PriorDebt", [2, 70m])];
        var interaction = Interaction(confirm: false);
        var model = Model(interaction);
        model.BeginDrag(_student);

        await model.DropAsync(_freeLocker);

        Assert.Equal([false], _requests.Select(r => r.ConfirmWarnings)); // never sent again with the warnings confirmed
        var only = Assert.Single(_notifications.Published);
        Assert.Equal(NotificationKind.Warning, only.Kind);
        Assert.Equal("S'ha cancel·lat l'operació. No s'ha canviat cap dada.", only.Text);
    }

    [Fact]
    [Trait("spec", Spec + ": Asignar arrastrando un alumno (Asignación arrastrando)")]
    public async Task A_business_error_is_explained_and_the_drag_is_over()
    {
        var interaction = new AssignLockerInteraction(
            (_, _) => Task.FromResult(Result<AssignLockerResult>.Failure(Arca.Domain.Lockers.LockerErrors.NotFound)),
            new RecordingConfirmations(true), _localizer, _notifications, _log, _delay);
        var model = Model(interaction);
        model.BeginDrag(_student);

        await model.DropAsync(_freeLocker);

        Assert.Equal(NotificationKind.Error, Assert.Single(_notifications.Published).Kind);
        Assert.False(model.IsDragging);
    }

    // --- Views ---

    [AvaloniaFact]
    [Trait("spec", Spec + ": Respuesta visual del destino (Destino válido)")]
    public void A_locker_becomes_a_drop_destination_and_a_student_row_a_drag_source()
    {
        var model = Model(Interaction());
        var locker = new Border();
        var row = new TextBlock();

        LockerDropTarget.Attach(locker, () => _freeLocker, model);
        StudentDragSource.Attach(row, () => _student, model);

        Assert.True(DragDrop.GetAllowDrop(locker));
        Assert.False(DragDrop.GetAllowDrop(row));
        Assert.False(model.IsDragging); // attaching starts nothing
    }
}
