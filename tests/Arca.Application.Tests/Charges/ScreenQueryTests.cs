// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using System.Text.Json;
using Arca.Application.Catalog.ListCatalog;
using Arca.Application.Lockers;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.RetireLocker;
using Arca.Application.Search;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Students.RetireStudent;
using Arca.Domain.Lockers;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class ScreenQueryTests
{
    const string Spec = "pantalles-de-domini/design";

    static async Task<PagamentsWorld> WorldAsync()
    {
        var world = new PagamentsWorld();
        await world.SeedAmountsAsync(await world.YearAsync(2026), 50m, 20m, 10m);
        return world;
    }

    ListLockerRowsHandler LockerRows(PagamentsWorld world) =>
        new(world.Store.Lockers, world.Store.Zones, world.Store.Assignments, world.Store.Students, world.Store.Charges);

    ListStudentRowsHandler StudentRows(PagamentsWorld world) =>
        new(world.Assignments.Students.Search, world.Store.Charges);

    // --- Lockers ---

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (taquilla con zona, estado visible, alumno y marca de deuda)")]
    public async Task The_locker_rows_come_composed_with_zone_status_holder_and_debt_and_the_retired_are_told_apart()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var occupied = await world.LockerAsync(1, zone);
        var broken = await world.LockerAsync(2, zone);
        var free = await world.LockerAsync(3, zone);
        var retired = await world.LockerAsync(4, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, occupied);
        await world.Assignments.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken), default);
        await world.Assignments.Inventory.RetireLocker.HandleAsync(new RetireLockerRequest(retired), default);

        var listing = (await LockerRows(world).HandleAsync(default)).Value!;

        Assert.Equal([1, 2, 3, 4], listing.Rows.Select(r => r.Number));
        Assert.Equal(
            [LockerStatusView.Occupied, LockerStatusView.Broken, LockerStatusView.Free, LockerStatusView.Retired],
            listing.Rows.Select(r => r.Status));
        Assert.All(listing.Rows, r => Assert.Equal("Planta 1", r.ZoneName));
        var first = listing.Rows[0];
        Assert.Equal("Marta Puig", first.StudentName);
        Assert.True(first.HasDebt);
        Assert.Null(listing.Rows[2].StudentName);
        Assert.Equal((3, 1, 1, 1), (listing.Counters.Active, listing.Counters.Free, listing.Counters.Occupied, listing.Counters.Broken)); // the retired one is not counted
        Assert.Equal(LockerEmptyState.None, listing.EmptyState);
        Assert.Equal(free, listing.Rows[2].Id);
    }

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (estados vacíos)")]
    public async Task Without_zones_or_without_lockers_the_listing_says_which_one_is_missing()
    {
        var world = await WorldAsync();

        Assert.Equal(LockerEmptyState.NoZones, (await LockerRows(world).HandleAsync(default)).Value!.EmptyState);

        await world.ZoneAsync("Planta 1");
        Assert.Equal(LockerEmptyState.NoLockers, (await LockerRows(world).HandleAsync(default)).Value!.EmptyState);
    }

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (los objetos no llevan correo ni identificador)")]
    public async Task The_lists_carry_no_email_and_no_identifier_of_any_student()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);

        var lockers = (await LockerRows(world).HandleAsync(default)).Value!;
        var students = (await StudentRows(world).HandleAsync(default)).Value!;

        foreach (var json in new[] { JsonSerializer.Serialize(lockers), JsonSerializer.Serialize(students) })
        {
            Assert.DoesNotContain("marta@example.com", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Email", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(student.Id.ToString(), JsonSerializer.Serialize(lockers), StringComparison.OrdinalIgnoreCase); // a locker row never names its student's identity
        }
    }

    // --- Students ---

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (alumno con taquilla y estado de pago)")]
    public async Task The_student_rows_carry_locker_and_what_is_owed_and_keep_the_retired_ones()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(7, zone);
        var marta = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        var pau = await world.StudentAsync("Pau", "Alsina", "pau@example.com");
        var oriol = await world.StudentAsync("Oriol", "Zamora", "oriol@example.com");
        await world.AssignAsync(marta.Id, locker);
        await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(oriol.Id, "Ha marxat"), default);

        var listing = (await StudentRows(world).HandleAsync(default)).Value!;

        Assert.Equal(["Alsina", "Puig", "Zamora"], listing.Rows.Select(r => r.LastName));
        var rowOfMarta = listing.Rows.Single(r => r.Id == marta.Id);
        Assert.Equal(7, rowOfMarta.LockerNumber);
        Assert.True(rowOfMarta.HasDebt);
        Assert.Equal(world.ChargesOf(marta.Id).Where(c => c.CountsAsDebt).Sum(c => c.Amount.Amount), rowOfMarta.PendingTotal);
        Assert.False(listing.Rows.Single(r => r.Id == pau.Id).HasDebt);
        Assert.True(listing.Rows.Single(r => r.Id == oriol.Id).IsRetired);
        Assert.Equal(2, listing.Counters.Active);
    }

    [Fact]
    [Trait("spec", Spec + ": D6 Consultas de lectura (sin curso activo)")]
    public async Task Without_an_active_year_the_student_rows_fail_with_the_error_the_screen_explains()
    {
        var world = new PagamentsWorld();

        var result = await StudentRows(world).HandleAsync(default);

        Assert.False(result.IsSuccess);
        Assert.Equal("SchoolYears.NoActiveYear", result.Error!.Code);
    }

    // --- Catalogue ---

    [Fact]
    [Trait("spec", Spec + ": D5 Selectores compartidos")]
    public async Task The_catalogue_lists_levels_and_groups_by_name_as_people_read_them()
    {
        var world = await WorldAsync();
        await world.StudentAsync("Marta", "Puig", "marta@example.com");

        var catalog = (await new ListCatalogHandler(world.Store.Catalog).HandleAsync(default)).Value!;

        Assert.NotEmpty(catalog.Levels);
        Assert.Equal(catalog.Levels.Order(Arca.Domain.Common.TextComparer.Comparer), catalog.Levels);
    }
}
