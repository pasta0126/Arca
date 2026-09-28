// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Charges;
using Arca.Application.Charges.AdjustChargeAmount;
using Arca.Application.Charges.ChargeKeyReplacement;
using Arca.Application.Charges.MarkChargeExempt;
using Arca.Application.Charges.GetLockerPayment;
using Arca.Application.Charges.GetStudentPayment;
using Arca.Application.Charges.ListDebtors;
using Arca.Application.Charges.ListDepositsDueBack;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.MarkDepositReturned;
using Arca.Application.Charges.ReturnDepositsInBulk;
using Arca.Application.Charges.RevertDepositReturn;
using Arca.Application.Charges.WaiveChargesInBulk;
using Arca.Application.Charges.RevertCharge;
using Arca.Application.Charges.VoidCharge;
using Arca.Application.Charges.WaiveCharge;
using Arca.Application.ConceptAmounts.SetConceptAmounts;
using Arca.Application.Students;
using Arca.Application.Tests.Assignments;
using Arca.Domain.Common;
using Arca.Testing;
using Arca.Testing.Inventory;

namespace Arca.Application.Tests.Charges;

/// <summary>
/// Every use case of the amounts by concept, the charges and the assignments wired together, so the hooks and the guard
/// that pagaments adds to alumnes-i-assignacions run exactly as they do in production.
/// </summary>
public sealed class PagamentsWorld
{
    public PagamentsWorld()
    {
        Assignments = new AssignmentsWorld();
        Assignments.Students.Guards.Add(new ChargeGenerationGuard(Store.ConceptAmounts, Store.Charges));
        Assignments.Students.OpenedHooks.Add(new ChargeGenerationHandler(Store.ConceptAmounts, Store.Charges, Store.ChargeEvents, Clock));
        Assignments.Students.LifecycleHooks.Add(new DepositLifecycleHandler(Store.Charges, Store.ChargeEvents, Clock));
    }

    public AssignmentsWorld Assignments { get; }

    public InMemoryInventory Store => Assignments.Store;

    public FakeClock Clock => Assignments.Clock;

    public MarkChargePaidHandler MarkPaid => new(Store.Charges, Store.ChargeEvents, Store.Students, Store.Years, Store, Clock);

    public MarkChargeExemptHandler MarkExempt => new(Store.Charges, Store.ChargeEvents, Store.Students, Store.Years, Store, Clock);

    public WaiveChargeHandler Waive => new(Store.Charges, Store.ChargeEvents, Store.Students, Store.Years, Store, Clock);

    public RevertChargeHandler Revert => new(Store.Charges, Store.ChargeEvents, Store.Students, Store.Years, Store, Clock);

    public VoidChargeHandler Void => new(Store.Charges, Store.ChargeEvents, Store.Students, Store.Years, Store, Clock);

    public AdjustChargeAmountHandler Adjust => new(Store.Charges, Store.ChargeEvents, Store.Students, Store.Years, Store, Clock);

    public MarkDepositReturnedHandler ReturnDeposit => new(Store.Charges, Store.ChargeEvents, Store.Students, Store.Years, Store, Clock);

    public RevertDepositReturnHandler RevertReturn => new(Store.Charges, Store.ChargeEvents, Store.Students, Store.Years, Store, Clock);

    public WaiveChargesInBulkHandler WaiveInBulk => new(Store.Charges, Store.ChargeEvents, Store, Clock);

    public ReturnDepositsInBulkHandler ReturnDepositsInBulk => new(Store.Charges, Store.ChargeEvents, Store, Clock);

    public GetStudentPaymentHandler StudentPayment => new(Store.Charges, Store.Students, Store.Years);

    public GetLockerPaymentHandler LockerPayment => new(Store.Assignments, Store.Charges, Store.Years);

    public ListDebtorsHandler Debtors => new(Store.Charges, Store.Students, Store.Enrollments, Store.Catalog, Store.Years, Assignments.Students.StudentLockers, Store.Lockers);

    public ListDepositsDueBackHandler DepositsDueBack => new(Store.Charges, Store.Students);

    public ChargeKeyReplacementHandler KeyReplacement => new(Store.Students, Store.Years, Store.ConceptAmounts, Store.Charges, Store.ChargeEvents, Store, Clock);

    public async Task<Guid> YearAsync(int startYear) => (await Assignments.Students.CreateYearAsync(startYear)).Value!.Id;

    public Task SetAmountsAsync(Guid yearId, decimal fee, decimal deposit, decimal keyReplacementFee) =>
        new SetConceptAmountsHandler(Store.Years, Store.ConceptAmounts, Store.ConceptAmountEvents, Store, Clock)
            .HandleAsync(new SetConceptAmountsRequest(yearId, fee, deposit, keyReplacementFee), default);

    /// <summary>Seeds amounts directly, bypassing the "year finished" guard, for a year a test treats as already past.</summary>
    public async Task SeedAmountsAsync(Guid yearId, decimal fee, decimal deposit, decimal keyReplacementFee)
    {
        foreach (var (concept, value) in new[] { (Domain.ConceptAmounts.ChargeConcept.Fee, fee), (Domain.ConceptAmounts.ChargeConcept.Deposit, deposit), (Domain.ConceptAmounts.ChargeConcept.KeyReplacementFee, keyReplacementFee) })
        {
            await Store.ConceptAmounts.AddAsync(
                Domain.ConceptAmounts.ConceptAmount.Create(Guid.NewGuid(), yearId, concept, value, Clock.UtcNow).Value!.Amount, default);
        }
    }

    public Task<Guid> ZoneAsync(string name) => Assignments.ZoneAsync(name);

    public Task<Guid> LockerAsync(int number, Guid zone) => Assignments.LockerAsync(number, zone);

    public Task<StudentDetail> StudentAsync(string first, string last, string email) => Assignments.StudentAsync(first, last, email);

    public Task<Result<AssignLockerResult>> AssignAsync(Guid studentId, Guid lockerId, bool confirm = false)
    {
        Clock.Advance(TimeSpan.FromMinutes(1));
        return Assignments.Assign.HandleAsync(new AssignLockerRequest(studentId, lockerId, confirm), default);
    }

    public IReadOnlyList<Domain.Charges.Charge> ChargesOf(Guid studentId) => [.. Store.ChargeList.Where(c => c.StudentId == studentId)];

    /// <summary>
    /// Seeds a pending charge directly for a year of a test's choosing, without going through an assignment: alumnes-i-
    /// assignacions has no way yet to have two active years at once (that is cursos-i-historial's job), so this is how a
    /// test gives a student debt "of a previous year" without needing that year to ever have been active.
    /// </summary>
    public async Task<Domain.Charges.Charge> SeedChargeAsync(Guid studentId, Domain.ConceptAmounts.ChargeConcept concept, Guid yearId, decimal amount)
    {
        var created = Domain.Charges.Charge.Create(Guid.NewGuid(), studentId, concept, yearId, Domain.Common.Money.FromCents((long)(amount * 100)), Clock.UtcNow);
        await Store.Charges.AddAsync(created.Charge, default);
        return created.Charge;
    }
}
