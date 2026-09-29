// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Identity;
using Arca.Application.Storage;
using Arca.Infrastructure.Tests.Storage;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Storage;
using Arca.Testing;
using Xunit;

namespace Arca.Infrastructure.Tests.Identity;

/// <summary>The identity of the centre over a real encrypted database: it is saved with the data, so it travels with a copy of the file.</summary>
public sealed class EfIdentityTests : IDisposable
{
    const string Spec = "ui-shell/identitat-i-tema";

    static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 9, 8, 7];

    readonly TempDirectory _dir = new();
    readonly DatabaseKey _key = TestKeys.FromSeed("identity");

    public void Dispose() => _dir.Dispose();

    [Fact]
    [Trait("spec", Spec + ": Identidad guardada con los datos (Restaurar en otro equipo)")]
    public async Task The_identity_is_saved_with_the_data_and_a_copy_of_the_file_shows_it_again()
    {
        var path = _dir.File("arca.db");
        var created = await ArcaDatabase.CreateAsync(path, _key);
        await created.Value!.DisposeAsync();
        var store = new EfInventory(() => new ArcaDbContext(path, _key));

        var saved = await new SaveCentreIdentityHandler(store.Identity, store)
            .HandleAsync(new SaveCentreIdentityRequest("Institut Exemple", LogoChange.Replace, _png, "#88AACC"), default);
        Assert.True(saved.IsSuccess);
        await new SaveCentreIdentityHandler(store.Identity, store).HandleAsync(new SaveCentreIdentityRequest("Institut Nou", LogoChange.Keep, null, "#88AACC"), default);

        // A backup is a copy of the file: opening the copy, as on another computer, gives the same identity.
        var copy = _dir.File("copy.db");
        File.Copy(path, copy);
        var other = new EfInventory(() => new ArcaDbContext(copy, _key));
        var read = (await new GetCentreIdentityHandler(other.Identity).HandleAsync(default)).Value!;

        Assert.Equal("Institut Nou", read.Name);
        Assert.Equal(_png, read.Logo);
        Assert.Equal("image/png", read.LogoContentType);
        Assert.Equal("#88AACC", read.Accent);
    }

    [Fact]
    [Trait("spec", Spec + ": Identidad guardada con los datos")]
    public async Task There_is_only_one_identity_however_many_times_it_is_saved()
    {
        var path = _dir.File("arca.db");
        var created = await ArcaDatabase.CreateAsync(path, _key);
        await created.Value!.DisposeAsync();
        var store = new EfInventory(() => new ArcaDbContext(path, _key));
        var handler = new SaveCentreIdentityHandler(store.Identity, store);

        await handler.HandleAsync(new SaveCentreIdentityRequest("A", LogoChange.Keep, null, null), default);
        await handler.HandleAsync(new SaveCentreIdentityRequest("B", LogoChange.Keep, null, null), default);

        await using var context = new ArcaDbContext(path, _key);
        Assert.Equal(1, await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.Set<Arca.Domain.Identity.CentreIdentity>()));
    }
}
