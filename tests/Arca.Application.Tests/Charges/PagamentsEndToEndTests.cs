// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.MarkDepositReturned;
using Arca.Application.Charges.ListDepositsDueBack;
using Arca.Application.Students.ReactivateStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class PagamentsEndToEndTests
{
    [Fact]
    [Trait("spec", "pagaments: recorrido completo de un alumno (asignación, pago, baja, devolución y reactivación)")]
    public async Task A_student_goes_from_assignment_to_a_returned_deposit_and_back_with_a_new_one()
    {
        var world = new PagamentsWorld();
        await world.SeedAmountsAsync(await world.YearAsync(2026), 50m, 20m, 10m);
        var zone = await world.ZoneAsync("Planta 1");
        var student = (await world.StudentAsync("Marta", "Puig", "marta@example.com")).Id;

        // A new student is assigned a locker: the fee and the deposit are generated, both pending.
        Assert.True((await world.AssignAsync(student, await world.LockerAsync(1, zone))).IsSuccess);
        var fee = world.ChargesOf(student).Single(c => c.Concept == ChargeConcept.Fee);
        var deposit = world.ChargesOf(student).Single(c => c.Concept == ChargeConcept.Deposit);
        Assert.All(new[] { fee, deposit }, c => Assert.Equal(ChargeStatus.Pending, c.Status));
        var locker = await LockerOf(world, student);
        Assert.False((await world.LockerPayment.HandleAsync(new(locker), default)).Value!.Standing!.UpToDate);

        // They pay both and are up to date.
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(fee.Id, null), default);
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(deposit.Id, null), default);
        Assert.True((await world.StudentPayment.HandleAsync(new(student), default)).Value!.Standing.UpToDate);

        // They leave: the deposit is due back and shows in the list; the fee, already paid, is untouched.
        Assert.True((await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(student, "Trasllat"), default)).IsSuccess);
        Assert.Equal(DepositReturnStatus.ToReturn, world.ChargesOf(student).Single(c => c.Id == deposit.Id).Return);
        var due = (await world.DepositsDueBack.HandleAsync(new ListDepositsDueBackRequest(), default)).Value!;
        Assert.Equal((1, 20m), (due.Count, due.TotalAmount));

        // The deposit is given back.
        Assert.True((await world.ReturnDeposit.HandleAsync(new MarkDepositReturnedRequest(deposit.Id, null, "En mà"), default)).IsSuccess);
        Assert.Empty((await world.DepositsDueBack.HandleAsync(new ListDepositsDueBackRequest(), default)).Value!.Rows);

        // They come back and get a locker: a new deposit is generated, and no second fee for the same year.
        Assert.True((await world.Assignments.Students.Reactivate.HandleAsync(new ReactivateStudentRequest(student, "1r ESO", "A"), default)).IsSuccess);
        Assert.True((await world.AssignAsync(student, await world.LockerAsync(2, zone))).IsSuccess);
        var deposits = world.ChargesOf(student).Where(c => c.Concept == ChargeConcept.Deposit).ToList();
        Assert.Equal(2, deposits.Count);
        Assert.Equal(DepositReturnStatus.Returned, deposits.Single(d => d.Id == deposit.Id).Return);
        Assert.Equal(ChargeStatus.Pending, deposits.Single(d => d.Id != deposit.Id).Status);
        Assert.Equal(1, world.ChargesOf(student).Count(c => c.Concept == ChargeConcept.Fee));
    }

    static async Task<Guid> LockerOf(PagamentsWorld world, Guid student) =>
        (await world.Store.Assignments.GetCurrentOfStudentAsync(student, default))!.LockerId;
}
