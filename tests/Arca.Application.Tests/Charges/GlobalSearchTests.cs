// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Search;
using Arca.Application.Students.RetireStudent;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class GlobalSearchTests
{
    const string Spec = "ui-shell/navegacio-i-cerca";

    static async Task<(PagamentsWorld World, Guid Zone)> WorldAsync(bool withYear = true)
    {
        var world = new PagamentsWorld();
        if (withYear)
        {
            await world.SeedAmountsAsync(await world.YearAsync(2026), 50m, 20m, 10m);
        }

        return (world, await world.ZoneAsync("Planta 1"));
    }

    static async Task<GlobalSearchResult> SearchAsync(PagamentsWorld world, string? text, bool retired = false, int max = 8) =>
        (await world.Search.HandleAsync(new GlobalSearchRequest(text, retired, max), default)).Value!;

    [Fact]
    [Trait("spec", Spec + ": Búsqueda global siempre visible (Buscar un alumno)")]
    public async Task A_student_is_found_ignoring_capitals_and_accents()
    {
        var (world, _) = await WorldAsync();
        await world.StudentAsync("Marta", "García Puig", "marta@example.com");
        await world.StudentAsync("Pau", "Abad", "pau@example.com");

        foreach (var text in new[] { "garcia", "GARCÍA", "  marta garcia  ", "puig" })
        {
            var result = await SearchAsync(world, text);

            Assert.Equal("García Puig", Assert.Single(result.Students).LastName);
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda global siempre visible (Buscar una taquilla)")]
    public async Task A_number_finds_the_locker_and_the_student_who_holds_it()
    {
        var (world, zone) = await WorldAsync();
        var locker = await world.LockerAsync(15, zone);
        await world.LockerAsync(150, zone);
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, locker);

        var result = await SearchAsync(world, "15");

        var hit = Assert.Single(result.Lockers);
        Assert.Equal((15, "Planta 1", LockerStatusView.Occupied, "Marta Puig"), (hit.Number, hit.ZoneName, hit.Status, hit.StudentName));
        Assert.Equal(15, Assert.Single(result.Students).LockerNumber); // and the student under Students too
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda global siempre visible (Buscar una taquilla)")]
    public async Task A_free_locker_is_found_as_free_with_no_student()
    {
        var (world, zone) = await WorldAsync();
        await world.LockerAsync(7, zone);

        var hit = Assert.Single((await SearchAsync(world, "7")).Lockers);

        Assert.Equal(LockerStatusView.Free, hit.Status);
        Assert.Null(hit.StudentName);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda global siempre visible (Buscar un grupo)")]
    public async Task A_group_is_found_with_its_level_and_the_number_of_active_students()
    {
        var (world, _) = await WorldAsync();
        await world.Assignments.StudentAsync("Marta", "Puig", "m@example.com", "1r ESO", "A");
        await world.Assignments.StudentAsync("Pau", "Abad", "p@example.com", "1r ESO", "A");
        await world.Assignments.StudentAsync("Joan", "Roca", "j@example.com", "2n ESO", "B");

        var result = await SearchAsync(world, "1r a");

        var group = Assert.Single(result.Groups);
        Assert.Equal(("1r ESO", "A", 2), (group.LevelName, group.GroupName, group.StudentCount));
        Assert.Equal(2, (await SearchAsync(world, "eso")).GroupTotal);
        Assert.Empty((await SearchAsync(world, "zz")).Groups);
    }

    [Fact]
    [Trait("spec", Spec + ": Resultados con estado visible (Alumno moroso)")]
    public async Task A_student_who_owes_shows_the_mark_and_the_amount_and_one_who_does_not_shows_none()
    {
        var (world, zone) = await WorldAsync();
        var owing = await world.StudentAsync("Marta", "Puig", "m@example.com");
        await world.StudentAsync("Pau", "Abad", "p@example.com");
        await world.AssignAsync(owing.Id, await world.LockerAsync(1, zone)); // fee 50 and deposit 20, both pending

        var result = await SearchAsync(world, "a");

        var debtor = result.Students.Single(s => s.LastName == "Puig");
        Assert.True(debtor.HasDebt);
        Assert.Equal(70m, debtor.PendingTotal);
        Assert.False(result.Students.Single(s => s.LastName == "Abad").HasDebt);
    }

    [Fact]
    [Trait("spec", Spec + ": Resultados con estado visible (Sin correo ni identificador)")]
    public void No_result_carries_an_email_or_an_identifier_of_the_student()
    {
        var forbidden = new[] { "Email", "Dni", "Identifier", "Note", "Reason" };
        foreach (var type in new[] { typeof(StudentHit), typeof(LockerHit), typeof(GroupHit), typeof(GlobalSearchResult) })
        {
            Assert.DoesNotContain(type.GetProperties(), p => forbidden.Any(f => p.Name.Contains(f, StringComparison.OrdinalIgnoreCase)));
        }
    }

    [Fact]
    [Trait("spec", Spec + ": Navegación de resultados (Muchos resultados)")]
    public async Task Each_kind_is_limited_and_reports_how_many_there_are_in_all()
    {
        var (world, _) = await WorldAsync();
        for (var i = 1; i <= 12; i++)
        {
            await world.StudentAsync("Alumne", $"Cognom {i:00}", $"a{i}@example.com");
        }

        var result = await SearchAsync(world, "alumne", max: 5);

        Assert.Equal(5, result.Students.Count);
        Assert.Equal(12, result.StudentTotal);
        Assert.Equal(["Cognom 01", "Cognom 02", "Cognom 03", "Cognom 04", "Cognom 05"], result.Students.Select(s => s.LastName));
    }

    [Fact]
    [Trait("spec", Spec + ": Ámbito de la búsqueda (Alumno de baja)")]
    public async Task A_student_who_left_is_not_found_unless_asked_and_the_search_says_how_many_are_hidden()
    {
        var (world, zone) = await WorldAsync();
        var gone = await world.StudentAsync("Marta", "Puig", "m@example.com");
        await world.AssignAsync(gone.Id, await world.LockerAsync(3, zone));
        await world.Assignments.Students.Retire.HandleAsync(new RetireStudentRequest(gone.Id, "Trasllat"), default);

        var hidden = await SearchAsync(world, "puig");
        var included = await SearchAsync(world, "puig", retired: true);

        Assert.Empty(hidden.Students);
        Assert.Equal(1, hidden.RetiredMatches);
        var hit = Assert.Single(included.Students);
        Assert.True(hit.IsRetired);
        Assert.Null(hit.LockerNumber); // a student who left holds no locker
        Assert.Equal(0, included.RetiredMatches);
    }

    [Fact]
    [Trait("spec", Spec + ": Navegación de resultados (Sin resultados)")]
    public async Task Nothing_typed_or_nothing_matching_gives_an_empty_result()
    {
        var (world, _) = await WorldAsync();
        await world.StudentAsync("Marta", "Puig", "m@example.com");

        Assert.True((await SearchAsync(world, null)).IsEmpty);
        Assert.True((await SearchAsync(world, "   ")).IsEmpty);
        Assert.True((await SearchAsync(world, "xyzzy")).IsEmpty);
    }

    [Fact]
    [Trait("spec", Spec + ": Ámbito de la búsqueda (Sin curso activo)")]
    public async Task Without_an_active_year_only_the_lockers_are_searched_and_nothing_fails()
    {
        var (world, zone) = await WorldAsync(withYear: false);
        await world.LockerAsync(15, zone);

        var result = await SearchAsync(world, "15");

        Assert.Single(result.Lockers);
        Assert.Empty(result.Students);
    }

    [Fact]
    [Trait("spec", Spec + ": Búsqueda sin bloquear")]
    public async Task Searching_writes_nothing()
    {
        var (world, zone) = await WorldAsync();
        await world.LockerAsync(1, zone);
        await world.StudentAsync("Marta", "Puig", "m@example.com");
        var (events, assignments, charges) = (world.Store.EventList.Count, world.Store.AssignmentList.Count, world.Store.ChargeList.Count);

        await SearchAsync(world, "marta");
        await SearchAsync(world, "1");

        Assert.Equal((events, assignments, charges), (world.Store.EventList.Count, world.Store.AssignmentList.Count, world.Store.ChargeList.Count));
    }
}
