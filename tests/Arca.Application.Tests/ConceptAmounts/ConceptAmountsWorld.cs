// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.ConceptAmounts.GetConceptAmounts;
using Arca.Application.ConceptAmounts.SetConceptAmounts;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Arca.Testing;
using Arca.Testing.Inventory;

namespace Arca.Application.Tests.ConceptAmounts;

/// <summary>The amounts by concept of a school year, wired over the in-memory inventory.</summary>
public sealed class ConceptAmountsWorld
{
    public InMemoryInventory Store { get; } = new();

    public FakeClock Clock { get; } = new(new DateTimeOffset(2026, 9, 27, 9, 0, 0, TimeSpan.Zero));

    public GetConceptAmountsHandler Get => new(Store.Years, Store.ConceptAmounts, Clock);

    public SetConceptAmountsHandler Set => new(Store.Years, Store.ConceptAmounts, Store.ConceptAmountEvents, Store, Clock);

    public async Task<Guid> YearAsync(int startYear)
    {
        var created = await new CreateAcademicYearHandler(Store.Years, Store)
            .HandleAsync(new(new DateOnly(startYear, 9, 1), new DateOnly(startYear + 1, 6, 30)), default);
        return created.Value!.Id;
    }

    public Task<Result<Application.ConceptAmounts.ConceptAmountsView>> SetAsync(Guid yearId, decimal fee, decimal deposit, decimal keyReplacementFee) =>
        Set.HandleAsync(new SetConceptAmountsRequest(yearId, fee, deposit, keyReplacementFee), default);

    /// <summary>Seeds amounts directly, bypassing the use case's own guard, for a year a test treats as already set up.</summary>
    public async Task SeedAsync(Guid yearId, decimal fee, decimal deposit, decimal keyReplacementFee)
    {
        foreach (var (concept, value) in new[] { (ChargeConcept.Fee, fee), (ChargeConcept.Deposit, deposit), (ChargeConcept.KeyReplacementFee, keyReplacementFee) })
        {
            await Store.ConceptAmounts.AddAsync(ConceptAmount.Create(Guid.NewGuid(), yearId, concept, value, Clock.UtcNow).Value!.Amount, default);
        }
    }
}
