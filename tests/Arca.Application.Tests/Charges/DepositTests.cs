// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.ReleaseStudentLocker;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.MarkChargeExempt;
using Arca.Application.Charges.MarkDepositReturned;
using Arca.Application.Charges.RevertCharge;
using Arca.Application.Charges.RevertDepositReturn;
using Arca.Application.Students.ReactivateStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.Charges;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class DepositTests
{
    const string Spec = "pagaments/fianca";

    static async Task<(PagamentsWorld World, Guid Zone, Guid Year)> WorldAsync()
    {
        var world = new PagamentsWorld();
        var year = await world.YearAsync(2026);
        await world.SeedAmountsAsync(year, 50m, 20m, 10m);
        return (world, await world.ZoneAsync("Planta 1"), year);
    }

    static Charge DepositOf(PagamentsWorld world, Guid studentId, Func<Charge, bool>? where = null) =>
        Assert.Single(world.ChargesOf(studentId), c => c.Concept == ChargeConcept.Deposit && (where?.Invoke(c) ?? true));

    static async Task<(PagamentsWorld World, Guid Zone, Guid StudentId, Guid Locker)> AssignedAsync()
    {
        var (world, zone, _) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        Assert.True((await world.AssignAsync(student.Id, locker)).IsSuccess);
        return (world, zone, student.Id, locker);
    }

    static async Task RetireAsync(PagamentsWorld world, Guid studentId) =>
        Assert.True((await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(studentId, "Trasllat"), default)).IsSuccess);

    static async Task ReactivateAsync(PagamentsWorld world, Guid studentId) =>
        Assert.True((await world.Assignments.Students.Reactivate.HandleAsync(new ReactivateStudentRequest(studentId, "1r ESO", "A"), default)).IsSuccess);

    [Fact]
    [Trait("spec", Spec + ": Una fianza vigente por alumno (Alumno con fianza vigente)")]
    public async Task A_second_assignment_never_generates_a_second_deposit()
    {
        var (world, zone, studentId, locker) = await AssignedAsync();
        await world.Assignments.Release.HandleAsync(new ReleaseStudentLockerRequest(studentId, null), default);

        await world.AssignAsync(studentId, await world.LockerAsync(2, zone));

        Assert.Equal(1, world.ChargesOf(studentId).Count(c => c.Concept == ChargeConcept.Deposit));
    }

    [Fact]
    [Trait("spec", Spec + ": La llave anual no afecta a la fianza (Liberación de la taquilla)")]
    public async Task Releasing_the_locker_leaves_a_paid_deposit_untouched()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(DepositOf(world, studentId).Id, null), default);

        await world.Assignments.Release.HandleAsync(new ReleaseStudentLockerRequest(studentId, null), default);

        var deposit = DepositOf(world, studentId);
        Assert.Equal(ChargeStatus.Paid, deposit.Status);
        Assert.Equal(DepositReturnStatus.None, deposit.Return);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja del alumno y fianza (Fianza pagada)")]
    public async Task Retiring_with_a_paid_deposit_leaves_it_due_back()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(DepositOf(world, studentId).Id, null), default);

        await RetireAsync(world, studentId);

        var deposit = DepositOf(world, studentId);
        Assert.Equal(ChargeStatus.Paid, deposit.Status);
        Assert.Equal(DepositReturnStatus.ToReturn, deposit.Return);
        Assert.Contains(world.Store.ChargeEventList, e => e.EntityId == deposit.Id && e.Type == ChargeEventTypes.ReturnDue);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja del alumno y fianza (Fianza pendiente)")]
    public async Task Retiring_with_a_pending_deposit_voids_it_with_the_retirement_reason()
    {
        var (world, _, studentId, _) = await AssignedAsync();

        await RetireAsync(world, studentId);

        var deposit = DepositOf(world, studentId);
        Assert.Equal(ChargeStatus.Voided, deposit.Status);
        Assert.Equal("Trasllat", deposit.Reason);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja del alumno y fianza (Fianza exenta o condonada)")]
    public async Task Retiring_with_an_exempt_deposit_changes_nothing()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        await world.MarkExempt.HandleAsync(new MarkChargeExemptRequest(DepositOf(world, studentId).Id, "Beca de menjador"), default);

        await RetireAsync(world, studentId);

        var deposit = DepositOf(world, studentId);
        Assert.Equal(ChargeStatus.Exempt, deposit.Status);
        Assert.Equal(DepositReturnStatus.None, deposit.Return);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja del alumno y fianza (Cuota pendiente)")]
    public async Task Retiring_leaves_a_pending_fee_pending()
    {
        var (world, _, studentId, _) = await AssignedAsync();

        await RetireAsync(world, studentId);

        Assert.Equal(ChargeStatus.Pending, Assert.Single(world.ChargesOf(studentId), c => c.Concept == ChargeConcept.Fee).Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Baja del alumno y fianza (Bajas masivas por importación)")]
    public async Task Retiring_many_students_leaves_every_paid_deposit_due_back_and_none_returned()
    {
        var (world, zone, _) = await WorldAsync();
        var students = new List<Guid>();
        for (var i = 1; i <= 5; i++)
        {
            var student = await world.StudentAsync("Alumne" + i, "Cognom" + i, $"a{i}@example.com");
            await world.AssignAsync(student.Id, await world.LockerAsync(i, zone));
            await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(DepositOf(world, student.Id).Id, null), default);
            students.Add(student.Id);
        }

        foreach (var id in students)
        {
            await RetireAsync(world, id);
        }

        Assert.All(students, id => Assert.Equal(DepositReturnStatus.ToReturn, DepositOf(world, id).Return));
    }

    [Fact]
    [Trait("spec", Spec + ": Devolución de la fianza (Devolución correcta)")]
    public async Task A_deposit_due_back_can_be_returned_with_its_date_and_note()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(DepositOf(world, studentId).Id, null), default);
        await RetireAsync(world, studentId);

        var result = await world.ReturnDeposit.HandleAsync(new MarkDepositReturnedRequest(DepositOf(world, studentId).Id, null, "En mà"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(DepositReturnStatus.Returned, result.Value!.Return);
        Assert.Equal("En mà", result.Value.ReturnNote);
        Assert.Contains(world.Store.ChargeEventList, e => e.Type == ChargeEventTypes.Returned);
    }

    [Fact]
    [Trait("spec", Spec + ": Devolución de la fianza (Fecha futura)")]
    public async Task A_future_return_date_is_rejected_and_nothing_changes()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(DepositOf(world, studentId).Id, null), default);
        await RetireAsync(world, studentId);

        var result = await world.ReturnDeposit.HandleAsync(
            new MarkDepositReturnedRequest(DepositOf(world, studentId).Id, world.Clock.Today.AddDays(1), null), default);

        Assert.Equal("Charges.DateInvalid", result.Error!.Code);
        Assert.Equal(DepositReturnStatus.ToReturn, DepositOf(world, studentId).Return);
    }

    [Fact]
    [Trait("spec", Spec + ": Devolución de la fianza (Alumno que sigue en el centro)")]
    public async Task The_deposit_of_an_active_student_cannot_be_returned()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(DepositOf(world, studentId).Id, null), default);

        var result = await world.ReturnDeposit.HandleAsync(new MarkDepositReturnedRequest(DepositOf(world, studentId).Id, null, null), default);

        Assert.Equal("Charges.StudentStillActive", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Devolución de la fianza (Fianza no pagada)")]
    public async Task A_deposit_that_was_never_paid_cannot_be_returned()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        await world.MarkExempt.HandleAsync(new MarkChargeExemptRequest(DepositOf(world, studentId).Id, "Beca"), default);
        await RetireAsync(world, studentId);

        var result = await world.ReturnDeposit.HandleAsync(new MarkDepositReturnedRequest(DepositOf(world, studentId).Id, null, null), default);

        Assert.Equal("Charges.InvalidStatus", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Corrección de una devolución (Devolución marcada por error)")]
    public async Task A_return_can_be_corrected_and_the_history_keeps_both_events()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        var id = DepositOf(world, studentId).Id;
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(id, null), default);
        await RetireAsync(world, studentId);
        await world.ReturnDeposit.HandleAsync(new MarkDepositReturnedRequest(id, null, null), default);

        var reverted = await world.RevertReturn.HandleAsync(new RevertDepositReturnRequest(id, "Marcada per error"), default);

        Assert.True(reverted.IsSuccess);
        Assert.Equal(DepositReturnStatus.ToReturn, reverted.Value!.Return);
        var types = world.Store.ChargeEventList.Where(e => e.EntityId == id).Select(e => e.Type).ToList();
        Assert.Contains(ChargeEventTypes.Returned, types);
        Assert.Contains(ChargeEventTypes.ReturnReverted, types);
    }

    [Fact]
    [Trait("spec", Spec + ": Corrección de una devolución (Revertir el pago de una devuelta)")]
    public async Task A_returned_deposit_cannot_be_reverted_to_pending()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        var id = DepositOf(world, studentId).Id;
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(id, null), default);
        await RetireAsync(world, studentId);
        await world.ReturnDeposit.HandleAsync(new MarkDepositReturnedRequest(id, null, null), default);

        var result = await world.Revert.HandleAsync(new RevertChargeRequest(id, "Error"), default);

        Assert.Equal("Charges.MustRevertReturnFirst", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Reactivación del alumno y fianza (Reactivado con la fianza aún por devolver)")]
    public async Task Reactivating_with_the_deposit_still_due_back_makes_it_paid_again()
    {
        var (world, _, studentId, _) = await AssignedAsync();
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(DepositOf(world, studentId).Id, null), default);
        await RetireAsync(world, studentId);

        await ReactivateAsync(world, studentId);

        var deposit = DepositOf(world, studentId);
        Assert.Equal(ChargeStatus.Paid, deposit.Status);
        Assert.Equal(DepositReturnStatus.None, deposit.Return);
        Assert.True(deposit.IsCurrentDeposit);
    }

    [Fact]
    [Trait("spec", Spec + ": Reactivación del alumno y fianza (Reactivado con la fianza devuelta)")]
    public async Task Reactivating_after_the_deposit_was_returned_generates_a_new_one_on_assignment()
    {
        var (world, zone, studentId, _) = await AssignedAsync();
        var first = DepositOf(world, studentId).Id;
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(first, null), default);
        await RetireAsync(world, studentId);
        await world.ReturnDeposit.HandleAsync(new MarkDepositReturnedRequest(first, null, null), default);
        await ReactivateAsync(world, studentId);

        await world.AssignAsync(studentId, await world.LockerAsync(2, zone));

        var deposits = world.ChargesOf(studentId).Where(c => c.Concept == ChargeConcept.Deposit).ToList();
        Assert.Equal(2, deposits.Count);
        Assert.Equal(DepositReturnStatus.Returned, deposits.Single(d => d.Id == first).Return);
        Assert.Equal(ChargeStatus.Pending, deposits.Single(d => d.Id != first).Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Reactivación del alumno y fianza (Reactivado con la fianza anulada)")]
    public async Task Reactivating_after_the_pending_deposit_was_voided_generates_a_new_one_on_assignment()
    {
        var (world, zone, studentId, _) = await AssignedAsync();
        var first = DepositOf(world, studentId).Id;
        await RetireAsync(world, studentId);
        await ReactivateAsync(world, studentId);

        await world.AssignAsync(studentId, await world.LockerAsync(2, zone));

        var deposits = world.ChargesOf(studentId).Where(c => c.Concept == ChargeConcept.Deposit).ToList();
        Assert.Equal(2, deposits.Count);
        Assert.Equal(ChargeStatus.Voided, deposits.Single(d => d.Id == first).Status);
        Assert.Equal(ChargeStatus.Pending, deposits.Single(d => d.Id != first).Status);
    }
}
