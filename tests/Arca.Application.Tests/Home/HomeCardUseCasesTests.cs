// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges.ListDebtors;
using Arca.Application.Home;
using Arca.Application.Localization;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Tests.Charges;
using Arca.Domain.Common;
using Arca.Domain.Home;
using Arca.Application.Lockers;
using Xunit;

namespace Arca.Application.Tests.Home;

public sealed class HomeCardUseCasesTests
{
    const string Spec = "filtres-i-targetes/targetes-d-inici";

    readonly ResxLocalizer _localizer = new();
    int _lockerReads;
    int _studentReads;

    static async Task<PagamentsWorld> WorldAsync()
    {
        var world = new PagamentsWorld();
        await world.SeedAmountsAsync(await world.YearAsync(2026), 50m, 20m, 10m);
        return world;
    }

    EnsureDefaultCardsHandler Ensure(PagamentsWorld world) => new(world.Store.HomeCards, world.Store, world.Clock, _localizer);

    GetHomeCardsHandler Cards(PagamentsWorld world)
    {
        var lockers = new ListLockerRowsHandler(world.Store.Lockers, world.Store.Zones, world.Store.Assignments, world.Store.Students, world.Store.Charges);
        var students = new ListStudentRowsHandler(world.Assignments.Students.Search, world.Store.Charges);
        return new GetHomeCardsHandler(
            world.Store.HomeCards, world.Store.Zones, world.Store.Catalog, world.Store.Years,
            ct =>
            {
                _lockerReads++;
                return lockers.HandleAsync(ct);
            },
            ct =>
            {
                _studentReads++;
                return students.HandleAsync(ct);
            });
    }

    static CreateHomeCardHandler Create(PagamentsWorld world) => new(world.Store.HomeCards, world.Store);

    static CreateHomeCardRequest Request(string title, HomeCardTargetView target = HomeCardTargetView.Students, params (string, string)[] criteria) =>
        new(title, target, criteria.ToDictionary(c => c.Item1, c => c.Item2));

    /// <summary>A zone with three lockers (two free, one held by a student who owes), two more students without a locker, one of them owing.</summary>
    static async Task<(Guid Zone, Guid Held, Guid Free)> CentreAsync(PagamentsWorld world)
    {
        var zone = await world.ZoneAsync("Planta 1");
        var held = await world.LockerAsync(1, zone);
        var free = await world.LockerAsync(2, zone);
        await world.LockerAsync(3, zone);
        var marta = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.StudentAsync("Pau", "Abad", "pau@example.com");
        await world.StudentAsync("Aina", "Zapata", "aina@example.com");
        await world.AssignAsync(marta.Id, held); // a pending fee and a pending deposit
        return (zone, held, free);
    }

    // --- The cards of a new centre ---

    [Fact]
    [Trait("spec", Spec + ": Tarjetas de serie (Centro nuevo)")]
    public async Task A_new_centre_gets_the_seven_default_cards_in_order_with_their_titles_once()
    {
        var world = await WorldAsync();

        var first = await Ensure(world).HandleAsync(default);
        var second = await Ensure(world).HandleAsync(default);

        Assert.Equal((7, 0), (first.Value, second.Value));
        var cards = await world.Store.HomeCards.ListAsync(default);
        Assert.Equal(
            ["Taquilles lliures", "Taquilles ocupades", "Taquilles reservades", "Taquilles avariades", "Taquilles en manteniment", "Alumnes sense taquilla", "Alumnes amb pendents de pagament"],
            cards.Select(c => c.Title));
        Assert.Equal([0, 1, 2, 3, 4, 5, 6], cards.Select(c => c.Position));
        Assert.All(cards.Take(5), c => Assert.Equal(HomeCardTarget.LockerMap, c.Target));
        Assert.All(cards.Skip(5), c => Assert.Equal(HomeCardTarget.Students, c.Target));
    }

    [Fact]
    [Trait("spec", Spec + ": Tarjetas de serie (Borrar una de serie)")]
    public async Task A_default_card_that_is_deleted_does_not_come_back_by_itself()
    {
        var world = await WorldAsync();
        await Ensure(world).HandleAsync(default);
        var reserved = (await world.Store.HomeCards.ListAsync(default)).Single(c => c.SeedKey == "lockers-reserved");
        await new DeleteHomeCardHandler(world.Store.HomeCards, world.Store).HandleAsync(new DeleteHomeCardRequest(reserved.Id), default);

        var again = await Ensure(world).HandleAsync(default); // what happens at every start

        Assert.Equal(0, again.Value);
        Assert.Equal(6, (await world.Store.HomeCards.ListAsync(default)).Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Tarjetas de serie (Restaurar)")]
    public async Task Restoring_adds_only_the_missing_ones_at_the_end_and_leaves_the_edited_ones_alone()
    {
        var world = await WorldAsync();
        await Ensure(world).HandleAsync(default);
        var cards = await world.Store.HomeCards.ListAsync(default);
        var broken = cards.Single(c => c.SeedKey == "lockers-broken");
        await new EditHomeCardHandler(world.Store.HomeCards, world.Store).HandleAsync(
            new EditHomeCardRequest(broken.Id, "Les que fallen", HomeCardTargetView.Lockers, new Dictionary<string, string> { ["Status"] = "Broken" }), default);
        var free = cards.Single(c => c.SeedKey == "lockers-free");
        await new DeleteHomeCardHandler(world.Store.HomeCards, world.Store).HandleAsync(new DeleteHomeCardRequest(free.Id), default);

        var restored = await Ensure(world).RestoreAsync(default);
        var twice = await Ensure(world).RestoreAsync(default);

        Assert.Equal((1, 0), (restored.Value, twice.Value));
        var after = await world.Store.HomeCards.ListAsync(default);
        Assert.Equal(7, after.Count);
        Assert.Equal("Taquilles lliures", after[^1].Title); // at the end
        Assert.Equal("Les que fallen", after.Single(c => c.SeedKey == "lockers-broken").Title); // the edit stays
        Assert.Equal([0, 1, 2, 3, 4, 5, 6], after.Select(c => c.Position));
    }

    // --- Create, edit, move, delete ---

    [Fact]
    [Trait("spec", Spec + ": Crear una tarjeta desde Inicio (Nueva tarjeta de taquillas averiadas de una zona)")]
    public async Task A_new_card_goes_at_the_end_with_its_title_and_criteria_and_a_wrong_one_is_refused_with_the_reason()
    {
        var world = await WorldAsync();
        var zone = await world.ZoneAsync("Planta 1");

        var saved = await Create(world).HandleAsync(Request("  Avariades de Planta 1 ", HomeCardTargetView.Lockers, ("Status", "Broken"), ("Zone", zone.ToString())), default);
        var refused = await Create(world).HandleAsync(Request("Dolenta", HomeCardTargetView.Lockers, ("Payment", "pending")), default);
        var empty = await Create(world).HandleAsync(Request(" "), default);

        Assert.Equal("Avariades de Planta 1", saved.Value!.Title);
        Assert.Equal("HomeCards.CriterionUnknown", refused.Error!.Code);
        Assert.Equal("HomeCards.TitleRequired", empty.Error!.Code);
        Assert.Single(await world.Store.HomeCards.ListAsync(default)); // what was refused left nothing
    }

    [Fact]
    [Trait("spec", Spec + ": Crear una tarjeta desde Inicio (Límite de tarjetas)")]
    public async Task There_cannot_be_more_than_24_cards_and_the_24th_fits()
    {
        var world = await WorldAsync();
        for (var i = 1; i <= 24; i++)
        {
            Assert.True((await Create(world).HandleAsync(Request("Targeta " + i), default)).IsSuccess);
        }

        var over = await Create(world).HandleAsync(Request("Una de més"), default);

        Assert.Equal("HomeCards.TooManyCards", over.Error!.Code);
        Assert.Equal(24, (await world.Store.HomeCards.ListAsync(default)).Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Editar, ordenar y borrar tarjetas (Editar)")]
    public async Task Editing_changes_a_card_and_a_refusal_or_a_missing_card_changes_nothing()
    {
        var world = await WorldAsync();
        var saved = (await Create(world).HandleAsync(Request("Sense taquilla", criteria: ("Locker", "without")), default)).Value!;
        var edit = new EditHomeCardHandler(world.Store.HomeCards, world.Store);

        Assert.True((await edit.HandleAsync(new EditHomeCardRequest(saved.Id, "Amb taquilla", HomeCardTargetView.Students, new Dictionary<string, string> { ["Locker"] = "with" }), default)).IsSuccess);
        var refused = await edit.HandleAsync(new EditHomeCardRequest(saved.Id, "X", HomeCardTargetView.Students, new Dictionary<string, string> { ["Locker"] = "maybe" }), default);
        var missing = await edit.HandleAsync(new EditHomeCardRequest(Guid.NewGuid(), "X", HomeCardTargetView.Students, new Dictionary<string, string>()), default);

        var card = Assert.Single(await world.Store.HomeCards.ListAsync(default));
        Assert.Equal(("Amb taquilla", "with"), (card.Title, card.Criteria["Locker"]));
        Assert.Equal("HomeCards.CriterionInvalid", refused.Error!.Code);
        Assert.Equal("HomeCards.NotFound", missing.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Editar, ordenar y borrar tarjetas (Mover, Primera tarjeta)")]
    public async Task A_card_moves_one_place_at_a_time_and_at_the_ends_it_stays()
    {
        var world = await WorldAsync();
        var ids = new List<Guid>();
        foreach (var title in new[] { "A", "B", "C" })
        {
            ids.Add((await Create(world).HandleAsync(Request(title), default)).Value!.Id);
        }

        var move = new MoveHomeCardHandler(world.Store.HomeCards, world.Store);
        Assert.True((await move.HandleAsync(new MoveHomeCardRequest(ids[2], HomeCardMove.Earlier), default)).Value);
        Assert.Equal(["A", "C", "B"], (await world.Store.HomeCards.ListAsync(default)).Select(c => c.Title));
        Assert.False((await move.HandleAsync(new MoveHomeCardRequest(ids[0], HomeCardMove.Earlier), default)).Value); // the first cannot go earlier
        Assert.False((await move.HandleAsync(new MoveHomeCardRequest(ids[1], HomeCardMove.Later), default)).Value); // nor the last later
        Assert.Equal([0, 1, 2], (await world.Store.HomeCards.ListAsync(default)).Select(c => c.Position));
        Assert.Equal("HomeCards.NotFound", (await move.HandleAsync(new MoveHomeCardRequest(Guid.NewGuid(), HomeCardMove.Later), default)).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Editar, ordenar y borrar tarjetas (Borrar)")]
    public async Task Deleting_a_card_numbers_the_others_again_and_touches_no_student_and_no_locker()
    {
        var world = await WorldAsync();
        await CentreAsync(world);
        foreach (var title in new[] { "A", "B", "C" })
        {
            await Create(world).HandleAsync(Request(title), default);
        }

        var b = (await world.Store.HomeCards.ListAsync(default)).Single(c => c.Title == "B");
        var studentsBefore = world.Store.StudentList.Count;
        var lockersBefore = world.Store.LockerList.Count;

        var deleted = await new DeleteHomeCardHandler(world.Store.HomeCards, world.Store).HandleAsync(new DeleteHomeCardRequest(b.Id), default);

        Assert.Equal("B", deleted.Value);
        var left = await world.Store.HomeCards.ListAsync(default);
        Assert.Equal(["A", "C"], left.Select(c => c.Title));
        Assert.Equal([0, 1], left.Select(c => c.Position));
        Assert.Equal((studentsBefore, lockersBefore), (world.Store.StudentList.Count, world.Store.LockerList.Count));
        Assert.Equal("HomeCards.NotFound", (await new DeleteHomeCardHandler(world.Store.HomeCards, world.Store).HandleAsync(new DeleteHomeCardRequest(b.Id), default)).Error!.Code);
    }

    // --- The counts ---

    [Fact]
    [Trait("spec", Spec + ": Recuento en vivo de la tarjeta (Recuento coherente con la pantalla)")]
    public async Task The_default_cards_count_what_other_queries_count_on_their_own()
    {
        var world = await WorldAsync();
        await CentreAsync(world);
        await Ensure(world).HandleAsync(default);

        var view = (await Cards(world).HandleAsync(default)).Value!;

        var byKey = view.Cards.ToDictionary(c => c.SeedKey!);
        var lockers = (await new ListLockerRowsHandler(world.Store.Lockers, world.Store.Zones, world.Store.Assignments, world.Store.Students, world.Store.Charges).HandleAsync(default)).Value!;
        Assert.Equal(("2026-2027", lockers.Counters.Free, lockers.Counters.Occupied, lockers.Counters.Reserved), (view.ActiveYearName, byKey["lockers-free"].Count, byKey["lockers-occupied"].Count, byKey["lockers-reserved"].Count));
        Assert.Equal((2, 1), (byKey["lockers-free"].Count, byKey["lockers-occupied"].Count));
        var search = (await world.Assignments.Students.Search.HandleAsync(new Arca.Application.Students.SearchStudents.SearchStudentsRequest(), default)).Value!;
        Assert.Equal(search.Counters.WithoutLocker, byKey["students-without-locker"].Count);
        var debtors = (await world.Debtors.HandleAsync(new ListDebtorsRequest(), default)).Value!;
        Assert.Equal(debtors.StudentCount, byKey["students-pending-payment"].Count);
        Assert.All(view.Cards, c => Assert.Equal(HomeCardState.WithCount, c.State));
    }

    [Fact]
    [Trait("spec", Spec + ": Recuento en vivo de la tarjeta (Tras un cambio)")]
    public async Task Paying_the_last_pending_charge_takes_a_student_off_the_pending_card()
    {
        var world = await WorldAsync();
        await CentreAsync(world);
        await Ensure(world).HandleAsync(default);
        var pending = (await Cards(world).HandleAsync(default)).Value!.Cards.Single(c => c.SeedKey == "students-pending-payment");
        Assert.Equal(1, pending.Count);

        foreach (var charge in world.Store.ChargeList.Where(c => c.Status == Arca.Domain.Charges.ChargeStatus.Pending).ToList())
        {
            await world.MarkPaid.HandleAsync(new Arca.Application.Charges.MarkChargePaid.MarkChargePaidRequest(charge.Id, null), default);
        }

        Assert.Equal(0, (await Cards(world).HandleAsync(default)).Value!.Cards.Single(c => c.SeedKey == "students-pending-payment").Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Recuento en vivo de la tarjeta (Muchas tarjetas)")]
    public async Task Twenty_cards_are_counted_with_one_read_of_the_lockers_and_one_of_the_students()
    {
        var world = await WorldAsync();
        await CentreAsync(world);
        for (var i = 1; i <= 10; i++)
        {
            await Create(world).HandleAsync(Request("Taquilles " + i, HomeCardTargetView.Lockers, ("Status", "Free")), default);
            await Create(world).HandleAsync(Request("Alumnes " + i, HomeCardTargetView.Students, ("Locker", "without")), default);
        }

        var view = (await Cards(world).HandleAsync(default)).Value!;

        Assert.Equal(20, view.Cards.Count);
        Assert.Equal((1, 1), (_lockerReads, _studentReads));
        Assert.All(view.Cards.Where(c => c.Title.StartsWith("Taquilles", StringComparison.Ordinal)), c => Assert.Equal(2, c.Count)); // the two that are free
        Assert.All(view.Cards.Where(c => c.Title.StartsWith("Alumnes", StringComparison.Ordinal)), c => Assert.Equal(2, c.Count));
    }

    [Fact]
    [Trait("spec", Spec + ": Inicio como panel de tarjetas (Centro sin configurar)")]
    public async Task The_view_says_whether_the_centre_has_lockers_and_students_to_tell_a_centre_not_set_up()
    {
        var world = await WorldAsync();
        await Create(world).HandleAsync(Request("Lliures", HomeCardTargetView.Lockers, ("Status", "Free")), default);
        var empty = (await Cards(world).HandleAsync(default)).Value!;

        await CentreAsync(world);
        var full = (await Cards(world).HandleAsync(default)).Value!;

        Assert.Equal((false, false), (empty.HasLockers, empty.HasStudents));
        Assert.Equal((true, true), (full.HasLockers, full.HasStudents));
    }

    [Fact]
    [Trait("spec", Spec + ": Recuento en vivo de la tarjeta (Sin curso activo)")]
    public async Task Without_an_active_year_the_students_cards_have_no_count_and_the_lockers_ones_still_count()
    {
        var world = new PagamentsWorld();
        var zone = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(1, zone);
        await Ensure(world).HandleAsync(default);

        var view = (await Cards(world).HandleAsync(default)).Value!;

        Assert.Null(view.ActiveYearName);
        Assert.Equal((1, HomeCardState.WithCount), (view.Cards.Single(c => c.SeedKey == "lockers-free").Count, view.Cards.Single(c => c.SeedKey == "lockers-free").State));
        Assert.All(view.Cards.Where(c => c.Target == HomeCardTargetView.Students), c => Assert.Equal((null, HomeCardState.NoCount), (c.Count, c.State)));
    }

    [Fact]
    [Trait("spec", Spec + ": Editar, ordenar y borrar tarjetas (Primera tarjeta)")]
    public async Task The_cards_say_whether_they_are_the_first_or_the_last_so_the_move_can_be_disabled()
    {
        var world = await WorldAsync();
        await Ensure(world).HandleAsync(default);

        var cards = (await Cards(world).HandleAsync(default)).Value!.Cards;

        Assert.Equal((true, false), (cards[0].IsFirst, cards[0].IsLast));
        Assert.Equal((false, true), (cards[^1].IsFirst, cards[^1].IsLast));
        Assert.DoesNotContain(cards.Skip(1).Take(5), c => c.IsFirst || c.IsLast);
    }

    // --- Filters that no longer exist ---

    [Fact]
    [Trait("spec", Spec + ": Tarjetas con filtros obsoletos (Zona que ya no existe)")]
    public async Task A_card_with_a_zone_that_no_longer_exists_is_marked_obsolete_counts_without_it_and_opens_without_it()
    {
        var world = await WorldAsync();
        await CentreAsync(world);
        var gone = Guid.NewGuid();
        var card = (await Create(world).HandleAsync(Request("De la zona vella", HomeCardTargetView.Lockers, ("Status", "Free"), ("Zone", gone.ToString())), default)).Value!;

        var view = (await Cards(world).HandleAsync(default)).Value!.Cards.Single();
        var resolved = (await new ResolveHomeCardHandler(world.Store.HomeCards, world.Store.Zones, world.Store.Catalog, Rows(world)).HandleAsync(card.Id, default)).Value!;

        Assert.Equal((HomeCardState.Obsolete, 2), (view.State, view.Count)); // the free ones of every zone
        Assert.Equal(["Zone"], view.Ignored);
        Assert.Equal(new Dictionary<string, string> { ["Status"] = "Free" }, view.Criteria);
        Assert.Equal(["Zone"], resolved.Ignored);
        Assert.Equal(new Dictionary<string, string> { ["Status"] = "Free" }, resolved.Criteria);
    }

    [Fact]
    [Trait("spec", Spec + ": Tarjetas con filtros obsoletos (Zona que ya no existe)")]
    public async Task A_zone_that_is_deactivated_with_no_lockers_is_obsolete_but_one_that_still_has_lockers_is_not()
    {
        var world = await WorldAsync();
        var used = await world.ZoneAsync("Planta 1");
        await world.LockerAsync(1, used);
        var empty = await world.ZoneAsync("Antic magatzem");
        world.Store.ZoneList.Single(z => z.Id == empty).Deactivate(0);
        world.Store.ZoneList.Single(z => z.Id == used).Deactivate(1); // deactivated but still holds a locker: nothing was lost
        await Create(world).HandleAsync(Request("Buida", HomeCardTargetView.Lockers, ("Zone", empty.ToString())), default);
        await Create(world).HandleAsync(Request("Amb taquilles", HomeCardTargetView.Lockers, ("Zone", used.ToString())), default);

        var cards = (await Cards(world).HandleAsync(default)).Value!.Cards;

        Assert.Equal(HomeCardState.Obsolete, cards.Single(c => c.Title == "Buida").State);
        Assert.Equal((HomeCardState.WithCount, 1), (cards.Single(c => c.Title == "Amb taquilles").State, cards.Single(c => c.Title == "Amb taquilles").Count));
    }

    [Fact]
    [Trait("spec", Spec + ": Tarjetas con filtros obsoletos (Abrir una tarjeta obsoleta)")]
    public async Task A_level_or_group_that_is_not_in_the_catalogue_is_ignored_and_named()
    {
        var world = await WorldAsync();
        await CentreAsync(world);
        var card = (await Create(world).HandleAsync(Request("De 3r", criteria: [("Level", "3r ESO"), ("Group", "A"), ("Locker", "without")]), default)).Value!;

        var resolved = (await new ResolveHomeCardHandler(world.Store.HomeCards, world.Store.Zones, world.Store.Catalog, Rows(world)).HandleAsync(card.Id, default)).Value!;

        Assert.Equal(["Level"], resolved.Ignored);
        Assert.Equal(["Group", "Locker"], resolved.Criteria.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("HomeCards.NotFound", (await new ResolveHomeCardHandler(world.Store.HomeCards, world.Store.Zones, world.Store.Catalog, Rows(world)).HandleAsync(Guid.NewGuid(), default)).Error!.Code);
    }

    Func<CancellationToken, Task<Result<LockerRowsListing>>> Rows(PagamentsWorld world) =>
        new ListLockerRowsHandler(world.Store.Lockers, world.Store.Zones, world.Store.Assignments, world.Store.Students, world.Store.Charges).HandleAsync;

    // --- The form and privacy ---

    [Fact]
    [Trait("spec", Spec + ": Crear una tarjeta desde Inicio (Vista previa del recuento)")]
    public async Task The_options_of_the_form_are_the_zones_in_use_and_the_levels_and_groups_of_the_catalogue_in_order()
    {
        var world = await WorldAsync();
        await CentreAsync(world);
        var idle = await world.ZoneAsync("Antic magatzem");
        world.Store.ZoneList.Single(z => z.Id == idle).Deactivate(0);
        await world.Assignments.StudentAsync("Jana", "Roca", "jana@example.com", "2n ESO", "B");

        var options = (await new GetCardOptionsHandler(world.Store.Zones, world.Store.Catalog).HandleAsync(default)).Value!;

        Assert.Equal(["Planta 1"], options.Zones.Select(z => z.Name));
        Assert.Equal(["1r ESO", "2n ESO"], options.Levels);
        Assert.Equal(["A", "B"], options.Groups);
    }

    [Fact]
    [Trait("spec", Spec + ": Una tarjeta es un filtro guardado (Sin datos personales)")]
    public void What_the_cards_show_holds_no_amount_no_email_and_no_name_of_a_student()
    {
        var forbidden = new[] { "Amount", "Total", "Email", "Dni", "Identifier", "Student", "Debt" };
        foreach (var type in new[] { typeof(HomeCardView), typeof(HomeCardsView), typeof(ResolvedHomeCard), typeof(CardOptions), typeof(HomeCardSaved) })
        {
            Assert.DoesNotContain(type.GetProperties().Where(p => p.Name != "HasStudents"), p => forbidden.Any(f => p.Name.Contains(f, StringComparison.OrdinalIgnoreCase))); // HasStudents is a yes or no, not a student
        }
    }
}
