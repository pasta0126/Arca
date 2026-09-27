// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.ChangeStudentLocker;
using Arca.Application.Assignments.ReleaseStudentLocker;
using Arca.Application.Charges.ChargeKeyReplacement;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class ChargeGenerationTests
{
    const string Spec = "pagaments/cobraments";

    static async Task<(PagamentsWorld World, Guid Zone)> WorldAsync(int startYear = 2026)
    {
        var world = new PagamentsWorld();
        var year = await world.YearAsync(startYear);
        await world.SeedAmountsAsync(year, 50m, 20m, 10m);
        var zone = await world.ZoneAsync("Planta 1");
        return (world, zone);
    }

    [Fact]
    [Trait("spec", Spec + ": Generación de la cuota al abrir la primera asignación del curso (Primera asignación del curso)")]
    public async Task Assigning_a_locker_generates_a_pending_fee_with_the_years_amount()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var result = await world.AssignAsync(student.Id, locker);

        Assert.True(result.IsSuccess);
        var fee = Assert.Single(world.ChargesOf(student.Id), c => c.Concept == ChargeConcept.Fee);
        Assert.Equal(ChargeStatus.Pending, fee.Status);
        Assert.Equal(50m, fee.Amount.Amount);
    }

    [Fact]
    [Trait("spec", Spec + ": Generación de la cuota al abrir la primera asignación del curso (Cambio de taquilla)")]
    public async Task Changing_locker_does_not_generate_a_new_fee()
    {
        var (world, zone) = await WorldAsync();
        var first = await world.LockerAsync(1, zone);
        var second = await world.LockerAsync(2, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, first);

        await world.Assignments.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, second), default);

        Assert.Single(world.ChargesOf(student.Id), c => c.Concept == ChargeConcept.Fee);
    }

    [Fact]
    [Trait("spec", Spec + ": Generación de la cuota al abrir la primera asignación del curso (Liberar y volver a asignar)")]
    public async Task Releasing_and_reassigning_keeps_the_existing_fee_with_no_new_one()
    {
        var (world, zone) = await WorldAsync();
        var first = await world.LockerAsync(1, zone);
        var second = await world.LockerAsync(2, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, first);

        await world.Assignments.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id), default);
        await world.AssignAsync(student.Id, second);

        Assert.Single(world.ChargesOf(student.Id), c => c.Concept == ChargeConcept.Fee);
    }

    [Fact]
    [Trait("spec", Spec + ": Generación de la cuota al abrir la primera asignación del curso (Alumno que llega a mitad de curso)")]
    public async Task A_student_who_arrives_halfway_through_the_year_gets_the_full_fee_with_no_proration()
    {
        var (world, zone) = await WorldAsync();
        world.Clock.Advance(TimeSpan.FromDays(120)); // well into the school year
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        await world.AssignAsync(student.Id, locker);

        var fee = Assert.Single(world.ChargesOf(student.Id), c => c.Concept == ChargeConcept.Fee);
        Assert.Equal(50m, fee.Amount.Amount);
    }

    [Fact]
    [Trait("spec", Spec + ": Generación de la cuota al abrir la primera asignación del curso (Alumno que se va antes de terminar)")]
    public async Task Releasing_the_locker_halfway_through_leaves_the_fee_pending()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);

        await world.Assignments.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id), default);

        var fee = Assert.Single(world.ChargesOf(student.Id), c => c.Concept == ChargeConcept.Fee);
        Assert.True(fee.CountsAsDebt);
    }

    [Fact]
    [Trait("spec", Spec + ": Generación de la cuota al abrir la primera asignación del curso (Importes sin definir)")]
    public async Task Assigning_with_no_amounts_defined_for_the_year_does_not_create_the_assignment()
    {
        var world = new PagamentsWorld();
        var year = await world.YearAsync(2026); // amounts never set
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var result = await world.AssignAsync(student.Id, locker);

        Assert.Equal("ConceptAmounts.NotDefined", result.Error!.Code);
        Assert.Empty(world.ChargesOf(student.Id));
        Assert.Null(world.Store.AssignmentList.SingleOrDefault(a => a.StudentId == student.Id));
    }

    // --- Key replacement (task 3.4) ---

    [Fact]
    [Trait("spec", Spec + ": Reposición de llave a demanda (Llave perdida con cobro)")]
    public async Task Charging_a_key_replacement_generates_a_pending_charge_with_the_years_amount()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);
        var year = world.ChargesOf(student.Id).Single(c => c.Concept == ChargeConcept.Fee).YearId;

        var result = await world.KeyReplacement.HandleAsync(new ChargeKeyReplacementRequest(student.Id, year), default);

        Assert.True(result.IsSuccess);
        var replacement = Assert.Single(world.ChargesOf(student.Id), c => c.Concept == ChargeConcept.KeyReplacementFee);
        Assert.Equal(10m, replacement.Amount.Amount);
    }

    [Fact]
    [Trait("spec", Spec + ": Reposición de llave a demanda (Llave perdida sin cobro)")]
    public async Task Not_charging_a_lost_key_generates_nothing()
    {
        var (world, _) = await WorldAsync();
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        // Nothing is called: the person decided not to charge for the key.

        Assert.Empty(world.ChargesOf(student.Id));
    }

    [Fact]
    [Trait("spec", Spec + ": Reposición de llave a demanda (Varias reposiciones)")]
    public async Task Charging_a_second_key_replacement_in_the_same_year_creates_a_separate_charge()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);
        var year = world.ChargesOf(student.Id).Single(c => c.Concept == ChargeConcept.Fee).YearId;

        var first = await world.KeyReplacement.HandleAsync(new ChargeKeyReplacementRequest(student.Id, year), default);
        var second = await world.KeyReplacement.HandleAsync(new ChargeKeyReplacementRequest(student.Id, year), default);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value!.Id, second.Value!.Id);
        Assert.Equal(2, world.ChargesOf(student.Id).Count(c => c.Concept == ChargeConcept.KeyReplacementFee));
    }

    // --- Managing charges of past years (task 3.6) ---

    [Fact]
    [Trait("spec", Spec + ": Gestión de cargos de cursos anteriores (Pagar una deuda antigua)")]
    public async Task An_old_pending_charge_can_be_marked_paid_even_though_its_year_is_not_active()
    {
        var (world, zone) = await WorldAsync(2020);
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);
        var oldFee = world.ChargesOf(student.Id).Single(c => c.Concept == ChargeConcept.Fee);

        var result = await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(oldFee.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChargeStatus.Paid, result.Value!.Status);
    }

    // --- Prior-year debt warning (task 3.5) ---

    [Fact]
    [Trait("spec", Spec + ": Deuda arrastrada y aviso al asignar (Aviso al asignar)")]
    public async Task Assigning_a_student_with_pending_charges_from_a_previous_year_warns_with_the_total()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var pastYear = Guid.NewGuid(); // a year that never had to be active for the student to owe it money
        await world.SeedChargeAsync(student.Id, ChargeConcept.Fee, pastYear, 50m);
        await world.SeedChargeAsync(student.Id, ChargeConcept.Deposit, pastYear, 20m);

        var result = await world.AssignAsync(student.Id, locker);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.NeedsConfirmation);
        var warning = Assert.Single(result.Value.Warnings);
        Assert.Equal("Charges.PriorDebt", warning.Code);
        Assert.Equal(2, warning.Args[0]); // the old fee and the old deposit
        Assert.Equal(70m, warning.Args[1]); // 50 + 20
    }

    [Fact]
    [Trait("spec", Spec + ": Deuda arrastrada y aviso al asignar (Aviso rechazado)")]
    public async Task Rejecting_the_prior_debt_warning_creates_no_assignment()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.SeedChargeAsync(student.Id, ChargeConcept.Fee, Guid.NewGuid(), 50m);

        var offered = await world.AssignAsync(student.Id, locker); // not confirmed

        Assert.True(offered.Value!.NeedsConfirmation);
        Assert.Null(world.Store.AssignmentList.SingleOrDefault(a => a.LockerId == locker));
    }

    [Fact]
    [Trait("spec", Spec + ": Deuda arrastrada y aviso al asignar (Sin deuda anterior)")]
    public async Task A_student_with_only_pending_charges_of_the_active_year_gets_no_warning()
    {
        var (world, zone) = await WorldAsync();
        var first = await world.LockerAsync(1, zone);
        var second = await world.LockerAsync(2, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, first);

        var result = await world.Assignments.Change.HandleAsync(new ChangeStudentLockerRequest(student.Id, second), default);

        Assert.Empty(result.Value!.Warnings);
    }

    [Fact]
    [Trait("spec", Spec + ": Deuda arrastrada y aviso al asignar (Deuda saldada)")]
    public async Task A_student_whose_prior_year_charges_are_all_settled_gets_no_warning()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var oldCharge = await world.SeedChargeAsync(student.Id, ChargeConcept.Fee, Guid.NewGuid(), 50m);
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(oldCharge.Id), default);

        var result = await world.AssignAsync(student.Id, locker);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.NeedsConfirmation);
    }
}
