// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.CheckAssignmentTarget;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Tests.Charges;
using Arca.Domain.Lockers;
using Xunit;

namespace Arca.Application.Tests.Assignments;

public sealed class CheckAssignmentTargetTests
{
    const string Spec = "ux-fonaments/arrossegar-i-deixar-anar: Respuesta visual del destino";

    static async Task<(AssignmentsWorld World, Guid Zone)> WorldAsync()
    {
        var world = new AssignmentsWorld();
        return (world, await world.ZoneAsync("Planta 1"));
    }

    [Fact]
    [Trait("spec", Spec + " (Destino válido)")]
    public async Task A_free_working_locker_is_a_valid_target_and_nothing_is_saved()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var check = (await world.CheckTarget.HandleAsync(new CheckAssignmentTargetRequest(student.Id, locker), default)).Value!;

        Assert.True(check.IsValid);
        Assert.Empty(check.Warnings);
        Assert.Empty(world.Store.AssignmentList);
        Assert.DoesNotContain(world.Store.StudentEventList, e => e.Type.Contains("Assigned", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec + " (Destino no válido)")]
    public async Task An_occupied_locker_is_not_a_valid_target_and_says_why()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var owner = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var other = await world.StudentAsync("Pau", "Abad", "pau@example.com");
        await world.AssignAsync(owner.Id, locker);

        var check = (await world.CheckTarget.HandleAsync(new CheckAssignmentTargetRequest(other.Id, locker), default)).Value!;

        Assert.False(check.IsValid);
        Assert.NotNull(check.Blocker);
    }

    [Fact]
    [Trait("spec", Spec + " (Destino no válido)")]
    public async Task A_broken_locker_is_not_a_valid_target()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(locker, OutOfServiceKind.Broken), default);

        var check = (await world.CheckTarget.HandleAsync(new CheckAssignmentTargetRequest(student.Id, locker), default)).Value!;

        Assert.False(check.IsValid);
    }

    [Fact]
    [Trait("spec", Spec + " (Destino no válido)")]
    public async Task A_student_who_already_has_a_locker_cannot_be_dropped_on_another()
    {
        var (world, zone) = await WorldAsync();
        var first = await world.LockerAsync(1, zone);
        var second = await world.LockerAsync(2, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, first);

        var check = (await world.CheckTarget.HandleAsync(new CheckAssignmentTargetRequest(student.Id, second), default)).Value!;

        Assert.False(check.IsValid);
    }

    [Fact]
    [Trait("spec", Spec + " (Destino no válido)")]
    public async Task Unknown_student_or_locker_is_answered_as_not_valid_instead_of_failing()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var noStudent = await world.CheckTarget.HandleAsync(new CheckAssignmentTargetRequest(Guid.NewGuid(), locker), default);
        var noLocker = await world.CheckTarget.HandleAsync(new CheckAssignmentTargetRequest(student.Id, Guid.NewGuid()), default);

        Assert.False(noStudent.Value!.IsValid);
        Assert.False(noLocker.Value!.IsValid);
    }

    [Fact]
    [Trait("spec", Spec + " (Destino válido)")]
    public async Task Debt_of_earlier_years_makes_the_target_valid_with_a_warning_to_confirm()
    {
        var world = new PagamentsWorld();
        var year = await world.YearAsync(2026);
        await world.SeedAmountsAsync(year, 50m, 20m, 10m);
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var previous = await world.YearAsync(2025);
        await world.SeedChargeAsync(student.Id, Domain.ConceptAmounts.ChargeConcept.Fee, previous, 50m);

        var check = (await world.Assignments.CheckTarget.HandleAsync(new CheckAssignmentTargetRequest(student.Id, locker), default)).Value!;

        Assert.True(check.IsValid);
        Assert.Equal("Charges.PriorDebt", Assert.Single(check.Warnings).Code);
        Assert.Empty(world.Store.AssignmentList);
        Assert.DoesNotContain(world.Store.ChargeList, c => c.Concept == Domain.ConceptAmounts.ChargeConcept.Deposit);
    }
}
