// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.ChangeStudentLocker;
using Arca.Application.Assignments.ReleaseStudentLocker;
using Xunit;

namespace Arca.Application.Tests.Assignments;

public sealed class AssignmentFeedbackTests
{
    const string Spec = "alumnes-i-assignacions/assignacions";

    // --- Results with texts ---

    [Fact]
    [Trait("spec", Spec + ": Feedback y guía en las asignaciones (Resultado)")]
    public async Task Assigning_a_locker_confirms_who_has_which_locker_and_in_which_zone()
    {
        var world = new AssignmentsWorld();
        var texts = new AssignmentResultTexts(world.Students.Localizer);
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(7, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var assigned = await world.AssignAsync(student.Id, locker);

        Assert.Equal("Marta Puig té ara la taquilla 7 a la zona Planta 1.", texts.Assigned(assigned.Assignment!));
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de taquilla (Cambio correcto)")]
    public async Task Changing_a_locker_confirms_the_new_locker_and_zone()
    {
        var world = new AssignmentsWorld();
        var texts = new AssignmentResultTexts(world.Students.Localizer);
        var zone = await world.ZoneAsync("Planta 1");
        var old = await world.LockerAsync(1, zone);
        var next = await world.LockerAsync(2, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, old);

        var changed = (await world.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, next), default)).Value!;

        Assert.Equal("Marta Puig ha canviat a la taquilla 2 a la zona Planta 1.", texts.Changed(changed.Assignment!));
    }

    [Fact]
    [Trait("spec", Spec + ": Liberación de una taquilla (Liberación manual)")]
    public async Task Releasing_a_locker_confirms_it_by_the_student_name()
    {
        var world = new AssignmentsWorld();
        var texts = new AssignmentResultTexts(world.Students.Localizer);
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);

        var released = (await world.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id), default)).Value!;

        Assert.Equal("S'ha alliberat la taquilla de Marta Puig.", texts.Released(released));
    }

    // --- Confirmations ---

    [Fact]
    [Trait("spec", Spec + ": Liberación de una taquilla (Confirmación)")]
    public async Task Releasing_asks_for_confirmation_saying_the_locker_will_be_free()
    {
        var world = new AssignmentsWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(9, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var assigned = await world.AssignAsync(student.Id, locker);

        var request = new AssignmentConfirmations(world.Students.Localizer).ForRelease(assigned.Assignment!);

        Assert.Equal("Alliberar la taquilla de Marta Puig?", request.Title);
        Assert.Equal("La taquilla 9 quedarà lliure.", request.Consequence);
        Assert.Equal("Allibera la taquilla", request.ConfirmLabel);
    }
}
