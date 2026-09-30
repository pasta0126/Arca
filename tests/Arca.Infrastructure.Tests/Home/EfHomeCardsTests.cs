// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;
using Arca.Domain.Home;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Tests.Storage;
using Arca.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Arca.Infrastructure.Tests.Home;

/// <summary>The cards of the start screen over a real encrypted database (targetes-d-inici, Persistencia y copia de seguridad).</summary>
public sealed class EfHomeCardsTests : IDisposable
{
    const string Spec = "filtres-i-targetes/targetes-d-inici";

    readonly TempDirectory _dir = new();
    readonly DatabaseKey _key = TestKeys.FromSeed("home-cards");

    public void Dispose() => _dir.Dispose();

    async Task<(string Path, EfInventory Store)> OpenAsync(string name = "arca.db")
    {
        var path = _dir.File(name);
        await (await ArcaDatabase.CreateAsync(path, _key)).Value!.DisposeAsync();
        return (path, new EfInventory(() => new ArcaDbContext(path, _key)));
    }

    static HomeCard Card(string title, int position, HomeCardTarget target = HomeCardTarget.Students, string? seed = null, params (string, string)[] criteria) =>
        HomeCard.Create(Guid.NewGuid(), title, target, criteria.ToDictionary(c => c.Item1, c => c.Item2), position, seed).Value!;

    static async Task AddAsync(EfInventory store, params HomeCard[] cards) => await store.RunAsync(async ct =>
    {
        foreach (var card in cards)
        {
            await store.HomeCards.AddAsync(card, ct);
        }

        return Result<bool>.Success(true);
    }, default);

    [Fact]
    [Trait("spec", Spec + ": Persistencia y copia de seguridad (Copia y restauración)")]
    public async Task The_cards_are_saved_in_their_order_and_a_copy_of_the_file_shows_the_same_cards_in_the_same_order()
    {
        var (path, store) = await OpenAsync();
        var zone = Guid.NewGuid();
        await AddAsync(
            store,
            Card("Sense taquilla", 1, criteria: ("Locker", "without")),
            Card("Lliures de la zona", 0, HomeCardTarget.LockerMap, "lockers-free", ("Status", "Free"), ("Zone", zone.ToString())),
            Card("Pendents", 2, criteria: ("Payment", "pending")));

        var copy = _dir.File("copy.db");
        File.Copy(path, copy); // a backup is a copy of the file: opening it, as on another computer, gives the same panel
        var other = new EfInventory(() => new ArcaDbContext(copy, _key));
        var read = await other.HomeCards.ListAsync(default);

        Assert.Equal(["Lliures de la zona", "Sense taquilla", "Pendents"], read.Select(c => c.Title));
        Assert.Equal((HomeCardTarget.LockerMap, "lockers-free"), (read[0].Target, read[0].SeedKey));
        Assert.Equal(new Dictionary<string, string> { ["Status"] = "Free", ["Zone"] = zone.ToString() }, read[0].Criteria);
        Assert.Null(read[1].SeedKey);
    }

    [Fact]
    [Trait("spec", Spec + ": Editar, ordenar y borrar tarjetas (Mover)")]
    public async Task Moving_two_cards_swaps_their_places_in_one_unit_of_work_and_it_survives_reopening()
    {
        var (path, store) = await OpenAsync();
        var first = Card("Primera", 0);
        var second = Card("Segona", 1);
        await AddAsync(store, first, second);

        await store.RunAsync(async ct =>
        {
            var a = (await store.HomeCards.GetAsync(first.Id, ct))!;
            var b = (await store.HomeCards.GetAsync(second.Id, ct))!;
            var place = a.Position;
            a.MoveTo(b.Position);
            b.MoveTo(place);
            await store.HomeCards.UpdateAsync(a, ct);
            return Result<bool>.Success(true);
        }, default);

        var reopened = new EfInventory(() => new ArcaDbContext(path, _key));
        Assert.Equal(["Segona", "Primera"], (await reopened.HomeCards.ListAsync(default)).Select(c => c.Title));
    }

    [Fact]
    [Trait("spec", Spec + ": Editar, ordenar y borrar tarjetas (Mover)")]
    public async Task A_move_that_fails_halfway_leaves_the_order_as_it_was()
    {
        var (_, store) = await OpenAsync();
        var first = Card("Primera", 0);
        var second = Card("Segona", 1);
        await AddAsync(store, first, second);

        var result = await store.RunAsync(async ct =>
        {
            var a = (await store.HomeCards.GetAsync(first.Id, ct))!;
            a.MoveTo(5);
            await store.HomeCards.UpdateAsync(a, ct);
            return Result<bool>.Failure(new Error("Test.Failure"));
        }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(["Primera", "Segona"], (await store.HomeCards.ListAsync(default)).Select(c => c.Title));
    }

    [Fact]
    [Trait("spec", Spec + ": Editar, ordenar y borrar tarjetas (Editar, Borrar)")]
    public async Task An_edited_card_keeps_its_changes_and_a_deleted_one_is_gone()
    {
        var (_, store) = await OpenAsync();
        var keep = Card("Lliures", 0, HomeCardTarget.LockerMap, null, ("Status", "Free"));
        var drop = Card("Esborrar", 1);
        await AddAsync(store, keep, drop);

        await store.RunAsync(async ct =>
        {
            var card = (await store.HomeCards.GetAsync(keep.Id, ct))!;
            card.Edit("Avariades", HomeCardTarget.Lockers, new Dictionary<string, string> { ["Status"] = "Broken" });
            await store.HomeCards.UpdateAsync(card, ct);
            await store.HomeCards.RemoveAsync((await store.HomeCards.GetAsync(drop.Id, ct))!, ct);
            return Result<bool>.Success(true);
        }, default);

        var read = Assert.Single(await store.HomeCards.ListAsync(default));
        Assert.Equal(("Avariades", HomeCardTarget.Lockers, "Broken"), (read.Title, read.Target, read.Criteria["Status"]));
    }

    [Fact]
    [Trait("spec", Spec + ": Tarjetas de serie (Borrar una de serie)")]
    public async Task The_cards_of_a_new_centre_are_remembered_as_created_once_and_a_second_mark_changes_nothing()
    {
        var (path, store) = await OpenAsync();
        Assert.Null(await store.HomeCards.GetDefaultsCreatedAtAsync(default));
        var first = new DateTimeOffset(2026, 9, 30, 10, 0, 0, TimeSpan.Zero);

        await store.RunAsync(async ct =>
        {
            await store.HomeCards.MarkDefaultsCreatedAsync(first, ct);
            await store.HomeCards.MarkDefaultsCreatedAsync(first.AddDays(1), ct);
            return Result<bool>.Success(true);
        }, default);

        Assert.Equal(first, await new EfInventory(() => new ArcaDbContext(path, _key)).HomeCards.GetDefaultsCreatedAtAsync(default));
        await using var context = new ArcaDbContext(path, _key);
        Assert.Equal(1, await context.Set<HomeCardsState>().CountAsync());
    }

    [Fact]
    [Trait("spec", Spec + ": Tarjetas de serie (Restaurar)")]
    public async Task Two_cards_cannot_share_a_seed_key_so_restoring_never_duplicates()
    {
        var (_, store) = await OpenAsync();
        await AddAsync(store, Card("Lliures", 0, HomeCardTarget.LockerMap, "lockers-free", ("Status", "Free")));

        await Assert.ThrowsAsync<DbUpdateException>(() => AddAsync(store, Card("Lliures 2", 1, HomeCardTarget.LockerMap, "lockers-free", ("Status", "Free"))));

        await AddAsync(store, Card("Una meva 1", 1), Card("Una meva 2", 2)); // the cards the person made have no key and can be any number
        Assert.Equal(3, (await store.HomeCards.ListAsync(default)).Count);
    }

    [Fact]
    [Trait("spec", Spec + ": Persistencia y copia de seguridad (Actualizar desde una versión sin tarjetas)")]
    public async Task Opening_a_centre_made_before_the_cards_adds_the_tables_without_losing_a_single_row()
    {
        var path = _dir.File("old.db");
        await using (var old = new ArcaDbContext(path, _key, create: true))
        {
            await old.GetService<IMigrator>().MigrateAsync("20260929200520_Identitat"); // the schema of the version before the cards
            await old.Database.ExecuteSqlRawAsync("INSERT INTO CentreIdentity (Id, Name) VALUES ('0d3f7a9e-5c1b-4a52-9e0d-1c2b3a4d5e6f', 'Institut Antic')");
            Assert.Equal(0, await old.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE name = 'HomeCards'").SingleAsync());
        }

        await using (var newer = new ArcaDbContext(path, _key))
        {
            await newer.Database.MigrateAsync();
        }

        await using var check = new ArcaDbContext(path, _key);
        Assert.Equal(2, await check.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE name IN ('HomeCards', 'HomeCardsState')").SingleAsync());
        Assert.Equal("Institut Antic", await check.Database.SqlQueryRaw<string>("SELECT Name AS Value FROM CentreIdentity").SingleAsync());
        Assert.Empty(await check.Set<HomeCard>().ToListAsync()); // the migration makes the tables and nothing more: the cards come from the use case
    }
}
