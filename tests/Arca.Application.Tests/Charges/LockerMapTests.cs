// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments.ReleaseStudentLocker;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.LockerMap;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.RetireLocker;
using Arca.Application.Search;
using Arca.Application.Zones.DeactivateZone;
using Arca.Domain.ConceptAmounts;
using Arca.Domain.Lockers;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class LockerMapTests
{
    const string Spec = "ui-shell/pantalla-principal";

    static async Task<PagamentsWorld> WorldAsync()
    {
        var world = new PagamentsWorld();
        await world.SeedAmountsAsync(await world.YearAsync(2026), 50m, 20m, 10m);
        return world;
    }

    static async Task<LockerMapData> MapAsync(PagamentsWorld world) => (await world.LockerMap.HandleAsync(default)).Value!;

    [Fact]
    [Trait("spec", Spec + ": Mapa de taquillas por zona (Mapa por zonas)")]
    public async Task The_map_has_each_zone_with_its_lockers_by_number_and_the_count_of_each_status()
    {
        var world = await WorldAsync();
        var first = await world.ZoneAsync("Planta 1");
        var second = await world.ZoneAsync("Planta 2");
        var ten = await world.LockerAsync(10, first);
        await world.LockerAsync(2, first);
        await world.LockerAsync(5, second);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, ten);

        var map = await MapAsync(world);

        Assert.Equal(["Planta 1", "Planta 2"], map.Zones.Select(z => z.ZoneName));
        Assert.Equal([2, 10], map.Zones[0].Lockers.Select(l => l.Number));
        Assert.Equal((3, 2, 1), (map.Counters.Active, map.Counters.Free, map.Counters.Occupied));
        Assert.Equal((2, 1, 1), (map.Zones[0].Counters.Active, map.Zones[0].Counters.Free, map.Zones[0].Counters.Occupied));
        Assert.False(map.IsEmpty);
    }

    [Fact]
    [Trait("spec", Spec + ": Mapa de taquillas por zona (Estado visible)")]
    public async Task Every_status_is_told_apart_and_the_holder_is_named()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var occupied = await world.LockerAsync(1, zone);
        var broken = await world.LockerAsync(2, zone);
        var maintenance = await world.LockerAsync(3, zone);
        await world.LockerAsync(4, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, occupied);
        await world.Assignments.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(broken, OutOfServiceKind.Broken), default);
        await world.Assignments.Inventory.MarkOutOfService.HandleAsync(new MarkLockerOutOfServiceRequest(maintenance, OutOfServiceKind.Maintenance), default);

        var lockers = (await MapAsync(world)).Zones.Single().Lockers;

        Assert.Equal(
            [LockerStatusView.Occupied, LockerStatusView.Broken, LockerStatusView.Maintenance, LockerStatusView.Free],
            lockers.Select(l => l.Status));
        Assert.Equal("Marta Puig", lockers[0].StudentName);
        Assert.Null(lockers[3].StudentName);
    }

    [Fact]
    [Trait("spec", Spec + ": Mapa de taquillas por zona (Taquilla ocupada con deuda)")]
    public async Task An_occupied_locker_of_a_student_who_owes_carries_the_debt_mark_until_they_pay()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);
        Assert.True((await MapAsync(world)).Zones.Single().Lockers.Single().HasDebt);

        foreach (var charge in world.ChargesOf(student.Id))
        {
            await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(charge.Id, null), default);
        }

        Assert.False((await MapAsync(world)).Zones.Single().Lockers.Single().HasDebt);
    }

    [Fact]
    [Trait("spec", Spec + ": Mapa de taquillas por zona (Zona desactivada)")]
    public async Task A_deactivated_zone_and_a_retired_locker_do_not_appear()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var gone = await world.ZoneAsync("Planta 2");
        await world.LockerAsync(1, zone);
        var retired = await world.LockerAsync(2, zone);
        await world.Assignments.Inventory.RetireLocker.HandleAsync(new RetireLockerRequest(retired), default);
        await world.Assignments.Inventory.DeactivateZone.HandleAsync(new DeactivateZoneRequest(gone), default);

        var map = await MapAsync(world);

        Assert.Equal(["Planta 1"], map.Zones.Select(z => z.ZoneName));
        Assert.Equal([1], map.Zones[0].Lockers.Select(l => l.Number));
    }

    [Fact]
    [Trait("spec", Spec + ": Mapa de taquillas por zona (Sin taquillas)")]
    public async Task Without_lockers_the_map_is_empty()
    {
        var world = await WorldAsync();
        await world.ZoneAsync("Planta 1");

        var map = await MapAsync(world);

        Assert.True(map.IsEmpty);
        Assert.Equal(0, map.Counters.Active);
        Assert.True((await new PagamentsWorld().LockerMap.HandleAsync(default)).Value!.IsEmpty);
    }

    [Fact]
    [Trait("spec", Spec + ": Carga y rendimiento del mapa (Actualización tras un cambio)")]
    public async Task One_locker_can_be_read_again_alone_after_a_change()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        Assert.Equal(LockerStatusView.Free, (await world.MapLocker.HandleAsync(locker, default)).Value!.Status);

        await world.AssignAsync(student.Id, locker);
        var assigned = (await world.MapLocker.HandleAsync(locker, default)).Value!;
        await world.Assignments.Release.HandleAsync(new ReleaseStudentLockerRequest(student.Id, null), default);
        var released = (await world.MapLocker.HandleAsync(locker, default)).Value!;

        Assert.Equal((LockerStatusView.Occupied, "Marta Puig", true), (assigned.Status, assigned.StudentName, assigned.HasDebt));
        Assert.Equal((LockerStatusView.Free, null, false), (released.Status, released.StudentName, released.HasDebt));
    }

    [Fact]
    [Trait("spec", Spec + ": Carga y rendimiento del mapa (Actualización tras un cambio)")]
    public async Task A_locker_that_no_longer_exists_or_was_retired_reads_as_gone()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(1, zone);
        await world.Assignments.Inventory.RetireLocker.HandleAsync(new RetireLockerRequest(locker), default);

        Assert.Null((await world.MapLocker.HandleAsync(locker, default)).Value);
        Assert.Null((await world.MapLocker.HandleAsync(Guid.NewGuid(), default)).Value);
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Correo e identificador)")]
    public void The_map_carries_no_email_and_no_identifier_of_the_student()
    {
        var forbidden = new[] { "Email", "Dni", "Identifier", "Note", "Reason" };
        foreach (var type in new[] { typeof(MapLocker), typeof(ZoneMap), typeof(LockerMapData) })
        {
            Assert.DoesNotContain(type.GetProperties(), p => forbidden.Any(f => p.Name.Contains(f, StringComparison.OrdinalIgnoreCase)));
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Carga y rendimiento del mapa (300 taquillas)")]
    public async Task A_map_of_600_lockers_is_built_in_one_query_without_writing_anything()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        for (var i = 1; i <= 600; i++)
        {
            await world.LockerAsync(i, zone);
        }

        var events = world.Store.EventList.Count;
        var map = await MapAsync(world);

        Assert.Equal(600, map.Counters.Free);
        Assert.Equal(events, world.Store.EventList.Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla ocupada)")]
    public async Task The_detail_of_an_occupied_locker_names_the_student_with_level_group_and_what_they_owe()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var locker = await world.LockerAsync(15, zone);
        var student = await world.Assignments.StudentAsync("Marta", "Puig", "marta@example.com", "2n ESO", "B");
        await world.AssignAsync(student.Id, locker);

        var detail = (await world.LockerDetail.HandleAsync(locker, default)).Value!;

        Assert.Equal((15, "Planta 1", LockerStatusView.Occupied), (detail.Number, detail.ZoneName, detail.Status));
        Assert.Equal(("Marta Puig", "2n ESO", "B"), (detail.StudentName, detail.LevelName, detail.GroupName));
        Assert.True(detail.HasDebt);
        Assert.Equal(70m, detail.PendingTotal);
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Taquilla libre)")]
    public async Task The_detail_of_a_free_locker_has_no_student_and_that_of_a_gone_one_is_null()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");
        var free = await world.LockerAsync(1, zone);
        var retired = await world.LockerAsync(2, zone);
        await world.Assignments.Inventory.RetireLocker.HandleAsync(new RetireLockerRequest(retired), default);

        var detail = (await world.LockerDetail.HandleAsync(free, default)).Value!;

        Assert.Equal(LockerStatusView.Free, detail.Status);
        Assert.Null(detail.StudentId);
        Assert.False(detail.HasDebt);
        Assert.Null((await world.LockerDetail.HandleAsync(retired, default)).Value);
        Assert.Null((await world.LockerDetail.HandleAsync(Guid.NewGuid(), default)).Value);
    }

    [Fact]
    [Trait("spec", Spec + ": Detalle de la taquilla seleccionada (Correo e identificador)")]
    public void The_detail_carries_no_email_and_no_identifier_of_the_student()
    {
        var forbidden = new[] { "Email", "Dni", "Identifier" };

        Assert.DoesNotContain(typeof(LockerDetail).GetProperties(), p => forbidden.Any(f => p.Name.Contains(f, StringComparison.OrdinalIgnoreCase)));
    }
}
