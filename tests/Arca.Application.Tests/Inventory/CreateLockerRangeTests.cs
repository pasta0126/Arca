// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Feedback;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.RetireLocker;
using Arca.Application.Zones.DeactivateZone;
using Arca.Domain.Lockers;
using Xunit;

namespace Arca.Application.Tests.Inventory;

public sealed class CreateLockerRangeTests
{
    const string Spec = "taquilles-i-zones/taquilles: Alta por rangos";

    sealed class Progress : IProgress<OperationProgress>
    {
        public List<OperationProgress> Reports { get; } = [];

        public void Report(OperationProgress value) => Reports.Add(value);
    }

    static async Task<CreateLockerRangePlan> AnalyzeAsync(InventoryWorld world, int first, int last, Guid zone) =>
        (await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(first, last, zone), null, default)).Value!;

    [Fact]
    [Trait("spec", Spec + " (Rango correcto)")]
    public async Task A_range_from_1_to_40_creates_40_free_lockers_each_with_its_creation_event()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var plan = await AnalyzeAsync(world, 1, 40, zone);

        var result = await world.CreateRange.ApplyAsync(plan, null, default);

        Assert.True(result.Value!.Applied);
        Assert.Equal(40, result.Value.Created);
        Assert.Equal(40, world.Store.LockerList.Count);
        Assert.Equal(Enumerable.Range(1, 40), world.Store.LockerList.Select(l => l.Number).Order());
        Assert.All(world.Store.LockerList, l => Assert.Equal(zone, l.ZoneId));
        Assert.Equal(40, world.Store.EventList.Count(e => e.Type == LockerEventTypes.Created));
        Assert.Equal(world.Store.LockerList.Select(l => l.Id).Order(), world.Store.EventList.Select(e => e.EntityId).Order());
    }

    [Fact]
    [Trait("spec", Spec + " (Vista previa)")]
    public async Task The_preview_says_how_many_would_be_created_and_saves_nothing()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(5, zone);
        var before = world.Store.LockerList.Count;

        var plan = await AnalyzeAsync(world, 1, 10, zone);

        Assert.Equal(10, plan.Count);
        Assert.Equal([5], plan.Conflicts);
        Assert.Equal("Planta 1", plan.ZoneName);
        Assert.Equal(before, world.Store.LockerList.Count);
        Assert.Single(world.Store.EventList);
    }

    [Fact]
    [Trait("spec", Spec + " (Vista previa)")]
    public async Task A_clean_preview_lists_the_numbers_it_would_create()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");

        var plan = await AnalyzeAsync(world, 3, 6, zone);

        Assert.False(plan.HasConflicts);
        Assert.Equal([3, 4, 5, 6], plan.ToCreate);
    }

    [Fact]
    [Trait("spec", Spec + " (Conflicto con números existentes)")]
    public async Task A_range_with_conflicts_creates_nothing_and_lists_every_conflicting_number()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(3, zone);
        await world.LockerAsync(7, zone);
        await world.LockerAsync(9, zone);
        var plan = await AnalyzeAsync(world, 1, 10, zone);

        var result = await world.CreateRange.ApplyAsync(plan, null, default);

        Assert.True(plan.HasConflicts);
        Assert.Equal([3, 7, 9], plan.Conflicts);
        Assert.Empty(plan.ToCreate);
        Assert.False(result.Value!.Applied);
        Assert.Equal(0, result.Value.Created);
        Assert.Equal(3, world.Store.LockerList.Count);
        var notice = Assert.Single(result.Notices);
        Assert.Equal("Lockers.RangeConflicts", notice.Code);
        Assert.Equal("3, 7, 9", Assert.Single(notice.Args));
    }

    [Fact]
    [Trait("spec", Spec + " (Conflicto con números existentes)")]
    public async Task The_number_of_a_retired_locker_is_not_a_conflict()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var old = await world.LockerAsync(5, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(old), default);

        var plan = await AnalyzeAsync(world, 1, 10, zone);

        Assert.False(plan.HasConflicts);
        Assert.Equal(10, plan.ToCreate.Count);
    }

    [Fact]
    [Trait("spec", Spec + " (Conflicto con números existentes)")]
    public async Task If_the_data_changes_after_the_preview_nothing_is_created_and_the_updated_plan_comes_back()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var plan = await AnalyzeAsync(world, 1, 10, zone);
        Assert.False(plan.HasConflicts);
        await world.LockerAsync(4, zone); // someone else takes a number after the preview

        var result = await world.CreateRange.ApplyAsync(plan, null, default);

        Assert.False(result.Value!.Applied);
        Assert.Equal([4], result.Value.Plan.Conflicts);
        Assert.Equal(["Lockers.RangeChanged", "Lockers.RangeConflicts"], result.Notices.Select(n => n.Code));
        Assert.Single(world.Store.LockerList);
    }

    [Fact]
    [Trait("spec", Spec + " (Conflicto con números existentes)")]
    public async Task A_previewed_conflict_that_clears_up_is_not_applied_by_itself()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var blocking = await world.LockerAsync(4, zone);
        var plan = await AnalyzeAsync(world, 1, 10, zone);
        Assert.Equal([4], plan.Conflicts);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(blocking), default); // the conflict goes away after the preview

        var result = await world.CreateRange.ApplyAsync(plan, null, default);

        Assert.False(result.Value!.Applied);
        Assert.Equal(0, result.Value.Created);
        Assert.Equal(10, result.Value.Plan.ToCreate.Count); // the plan that can now be confirmed
        Assert.Equal(["Lockers.RangeChanged"], result.Notices.Select(n => n.Code));
        Assert.Single(world.Store.LockerList); // only the retired one, nothing new was created
    }

    [Fact]
    [Trait("spec", Spec + " (Conflicto con números existentes)")]
    public async Task Confirming_the_updated_plan_after_a_change_then_applies_it()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var blocking = await world.LockerAsync(4, zone);
        var plan = await AnalyzeAsync(world, 1, 10, zone);
        await world.RetireLocker.HandleAsync(new RetireLockerRequest(blocking), default);
        var updated = (await world.CreateRange.ApplyAsync(plan, null, default)).Value!.Plan;

        var result = await world.CreateRange.ApplyAsync(updated, null, default);

        Assert.True(result.Value!.Applied);
        Assert.Equal(10, result.Value.Created);
    }

    [Fact]
    [Trait("spec", Spec + " (Rango invertido)")]
    public async Task A_range_with_the_first_number_greater_than_the_last_is_refused()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");

        var result = await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(20, 10, zone), null, default);

        Assert.Equal("Lockers.RangeInvalid", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + " (Rango demasiado grande)")]
    public async Task A_range_of_more_than_1000_lockers_is_refused_and_1000_is_accepted()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");

        var tooBig = await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(1, 1001, zone), null, default);
        var exact = await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(1, 1000, zone), null, default);

        Assert.Equal("Lockers.RangeTooLarge", tooBig.Error!.Code);
        Assert.Equal(1000, Assert.Single(tooBig.Error.Args));
        Assert.Equal(1000, exact.Value!.ToCreate.Count);
    }

    [Fact]
    [Trait("spec", Spec + " (Rango de un solo número)")]
    public async Task A_range_of_one_number_creates_a_single_locker()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var plan = await AnalyzeAsync(world, 7, 7, zone);

        var result = await world.CreateRange.ApplyAsync(plan, null, default);

        Assert.Equal(1, plan.Count);
        Assert.Equal(1, result.Value!.Created);
        Assert.Equal(7, Assert.Single(world.Store.LockerList).Number);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-5, 10)]
    [InlineData(1, 100000)]
    [Trait("spec", Spec + " (Rango invertido)")]
    public async Task A_number_outside_the_allowed_limits_is_refused(int first, int last)
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");

        var result = await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(first, last, zone), null, default);

        Assert.Equal("Lockers.NumberInvalid", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + " (Rango correcto)")]
    public async Task The_zone_must_exist_and_be_active()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Antiga");
        await world.DeactivateZone.HandleAsync(new DeactivateZoneRequest(zone), default);

        var inactive = await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(1, 5, zone), null, default);
        var unknown = await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(1, 5, Guid.NewGuid()), null, default);

        Assert.Equal("Lockers.ZoneUnavailable", inactive.Error!.Code);
        Assert.Equal("Zones.NotFound", unknown.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + " (Fallo durante la creación)")]
    public async Task A_failure_in_the_middle_of_saving_leaves_no_locker_of_the_range()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var plan = await AnalyzeAsync(world, 1, 40, zone);
        world.Store.EventsBeforeFailure = 20;

        await Assert.ThrowsAsync<IOException>(() => world.CreateRange.ApplyAsync(plan, null, default));

        Assert.Empty(world.Store.LockerList);
        Assert.Empty(world.Store.EventList);
    }

    [Fact]
    [Trait("spec", Spec + " (Vista previa)")]
    public async Task Cancelling_the_analysis_creates_nothing()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(1, 500, zone), null, cancellation.Token));

        Assert.Empty(world.Store.LockerList);
    }

    [Fact]
    [Trait("spec", Spec + " (Rango correcto)")]
    public async Task The_analysis_reports_progress_and_the_saving_says_it_can_no_longer_be_cancelled()
    {
        var world = new InventoryWorld();
        var zone = await world.ZoneAsync("Planta 1");
        var analysis = new Progress();
        var saving = new Progress();

        var plan = (await world.CreateRange.AnalyzeAsync(new CreateLockerRangeRequest(1, 250, zone), analysis, default)).Value!;
        await world.CreateRange.ApplyAsync(plan, saving, default);

        Assert.Equal(250, analysis.Reports[^1].Total);
        Assert.Equal(250, analysis.Reports[^1].Current);
        Assert.Contains(analysis.Reports, r => r.Current == 100 && r.CanCancel);
        Assert.Contains(saving.Reports, r => r.Total == 250 && !r.CanCancel);
        Assert.Equal(250, saving.Reports[^1].Current);
    }
}
