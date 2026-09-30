// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Charges;
using Arca.Application.Charges.AdjustChargeAmount;
using Arca.Application.Charges.ChargeKeyReplacement;
using Arca.Application.Charges.GetChargeHistory;
using Arca.Application.Charges.GetStudentChargesScreen;
using Arca.Application.Charges.MarkChargeExempt;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.RevertCharge;
using Arca.Application.Charges.VoidCharge;
using Arca.Application.Charges.WaiveCharge;
using Arca.Application.Common;
using Arca.Application.Localization;
using Arca.Application.SchoolYears.ListAcademicYears;
using Arca.Application.Students;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Students.SearchStudents;
using Arca.Application.Zones.ListZoneRows;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Arca.Infrastructure.Inventory;
using Arca.UI.Charges;

namespace Arca.Desktop.Composition;

/// <summary>
/// Joins the charges of the record of a student to their use cases (the composition root is the only place of the desktop project that knows them): each
/// operation answers with the sentence that says what was done, made from the same result texts the rest of the application uses.
/// </summary>
static class ChargesComposition
{
    public static ChargeServices Create(EfInventory store, IClock clock, ILocalizer localizer)
    {
        var texts = new ChargeResultTexts(localizer);
        var paid = new MarkChargePaidHandler(store.Charges, store.ChargeEvents, store.Students, store.Years, store, clock);
        var exempt = new MarkChargeExemptHandler(store.Charges, store.ChargeEvents, store.Students, store.Years, store, clock);
        var waive = new WaiveChargeHandler(store.Charges, store.ChargeEvents, store.Students, store.Years, store, clock);
        var voided = new VoidChargeHandler(store.Charges, store.ChargeEvents, store.Students, store.Years, store, clock);
        var revert = new RevertChargeHandler(store.Charges, store.ChargeEvents, store.Students, store.Years, store, clock);
        var adjust = new AdjustChargeAmountHandler(store.Charges, store.ChargeEvents, store.Students, store.Years, store, clock);
        var key = new ChargeKeyReplacementHandler(store.Students, store.Years, store.ConceptAmounts, store.Charges, store.ChargeEvents, store, clock);

        static async Task<Result<string>> Said<T>(Task<Result<T>> run, Func<T, string> say)
        {
            var result = await run;
            return result.IsSuccess ? Result<string>.Success(say(result.Value!), [.. result.Notices]) : Result<string>.Failure(result.Error!);
        }

        return new ChargeServices(
            (id, ct) => Screen(store, clock).HandleAsync(new GetStudentChargesScreenRequest(id), ct),
            async (id, ct) =>
            {
                var history = await new GetChargeHistoryHandler(store.Charges, store.ChargeEvents, localizer).HandleAsync(new GetChargeHistoryRequest(id), ct);
                return history.IsSuccess
                    ? Result<IReadOnlyList<string>>.Success([.. history.Value!.Select(l => $"{localizer.Format(l.At)}: {l.Text}")])
                    : Result<IReadOnlyList<string>>.Failure(history.Error!);
            },
            (id, date, ct) => Said(paid.HandleAsync(new MarkChargePaidRequest(id, date), ct), texts.Paid),
            (id, reason, ct) => Said(exempt.HandleAsync(new MarkChargeExemptRequest(id, reason), ct), texts.Exempted),
            (id, reason, ct) => Said(waive.HandleAsync(new WaiveChargeRequest(id, reason), ct), texts.Waived),
            (id, reason, ct) => Said(voided.HandleAsync(new VoidChargeRequest(id, reason), ct), texts.Voided),
            (id, reason, ct) => Said(revert.HandleAsync(new RevertChargeRequest(id, reason), ct), texts.Reverted),
            (id, amount, reason, ct) => Said(adjust.HandleAsync(new AdjustChargeAmountRequest(id, amount, reason), ct), texts.AmountAdjusted),
            (student, year, ct) => Said(key.HandleAsync(new ChargeKeyReplacementRequest(student, year), ct), texts.KeyReplacementCharged),
            () => clock.Today);
    }

    static GetStudentChargesScreenHandler Screen(EfInventory store, IClock clock) =>
        new(store.Charges, store.Students, store.Years, store.ConceptAmounts, clock);

    /// <summary>The debt of a student from previous years by concept and year, in words, for the warning shown before assigning them a locker.</summary>
    public static Func<Guid, CancellationToken, Task<IReadOnlyList<string>>> DebtLines(EfInventory store, IClock clock, ILocalizer localizer) => async (id, ct) =>
    {
        var screen = await Screen(store, clock).HandleAsync(new GetStudentChargesScreenRequest(id), ct);
        return screen.IsSuccess
            ? [.. screen.Value!.Lines.Where(l => l.Status == "Pending" && !l.IsActiveYear).Select(l => localizer.Get(
                "Charges.Label.DebtLine", Arca.Application.ConceptAmounts.ConceptNames.Of(localizer, l.Concept), l.YearName, localizer.Format(Money.FromCents((long)Math.Round(l.Amount * 100)))))]
            : [];
    };
}
