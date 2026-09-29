// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Identity;
using Arca.Testing.Inventory;
using Xunit;

namespace Arca.Application.Tests.Identity;

public sealed class CentreIdentityTests
{
    const string Spec = "ui-shell/identitat-i-tema";

    static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    readonly InMemoryInventory _store = new();

    SaveCentreIdentityHandler Save => new(_store.Identity, _store);

    [Fact]
    [Trait("spec", Spec + ": Nombre del centro (Definir el nombre, Sin nombre definido)")]
    public async Task The_name_is_saved_trimmed_and_without_any_it_is_empty()
    {
        Assert.Equal(CentreIdentityView.Empty, (await new GetCentreIdentityHandler(_store.Identity).HandleAsync(default)).Value);

        var saved = await Save.HandleAsync(new SaveCentreIdentityRequest("  Institut Exemple ", LogoChange.Keep, null, null), default);

        Assert.Equal("Institut Exemple", saved.Value!.Name);
        Assert.Equal("Institut Exemple", (await new GetCentreIdentityHandler(_store.Identity).HandleAsync(default)).Value!.Name);
    }

    [Theory]
    [Trait("spec", Spec + ": Nombre del centro (Nombre vacío)")]
    [InlineData("", "Identity.NameRequired")]
    [InlineData("   ", "Identity.NameRequired")]
    [InlineData(null, "Identity.NameRequired")]
    public async Task An_empty_name_is_refused_and_nothing_is_saved(string? name, string code)
    {
        var result = await Save.HandleAsync(new SaveCentreIdentityRequest(name, LogoChange.Keep, null, null), default);

        Assert.Equal(code, result.Error!.Code);
        Assert.Null(_store.StoredIdentity);
    }

    [Fact]
    [Trait("spec", Spec + ": Nombre del centro")]
    public async Task A_name_over_100_characters_is_refused()
    {
        var result = await Save.HandleAsync(new SaveCentreIdentityRequest(new string('a', 101), LogoChange.Keep, null, null), default);

        Assert.Equal("Identity.NameTooLong", result.Error!.Code);
    }

    [Theory]
    [Trait("spec", Spec + ": Logo del centro (Cargar un logo)")]
    [InlineData(true, "image/png")]
    [InlineData(false, "image/jpeg")]
    public async Task A_png_or_a_jpeg_is_accepted_by_what_it_is(bool png, string type)
    {
        var result = await Save.HandleAsync(new SaveCentreIdentityRequest("Centre", LogoChange.Replace, png ? _png : _jpeg, null), default);

        Assert.Equal(type, result.Value!.LogoContentType);
        Assert.NotNull(result.Value.Logo);
    }

    [Fact]
    [Trait("spec", Spec + ": Logo del centro (Formato no admitido)")]
    public async Task A_file_that_is_not_a_png_or_a_jpeg_is_refused_whatever_its_name_and_the_current_logo_stays()
    {
        await Save.HandleAsync(new SaveCentreIdentityRequest("Centre", LogoChange.Replace, _png, null), default);

        var result = await Save.HandleAsync(new SaveCentreIdentityRequest("Altre nom", LogoChange.Replace, "GIF89a...."u8.ToArray(), null), default);

        Assert.Equal("Identity.LogoFormatNotSupported", result.Error!.Code);
        Assert.Equal("Centre", _store.StoredIdentity!.Name); // nothing at all changed
        Assert.Equal(_png, _store.StoredIdentity.Logo);
    }

    [Fact]
    [Trait("spec", Spec + ": Logo del centro (Fichero demasiado grande)")]
    public async Task A_logo_over_one_megabyte_is_refused_with_the_maximum()
    {
        var big = new byte[(1024 * 1024) + 1];
        _png.CopyTo(big, 0);

        var result = await Save.HandleAsync(new SaveCentreIdentityRequest("Centre", LogoChange.Replace, big, null), default);

        Assert.Equal("Identity.LogoTooLarge", result.Error!.Code);
        Assert.Equal(1, Assert.Single(result.Error.Args));
    }

    [Fact]
    [Trait("spec", Spec + ": Logo del centro (Quitar el logo)")]
    public async Task Removing_the_logo_leaves_only_the_name()
    {
        await Save.HandleAsync(new SaveCentreIdentityRequest("Centre", LogoChange.Replace, _png, null), default);

        var result = await Save.HandleAsync(new SaveCentreIdentityRequest("Centre", LogoChange.Remove, null, null), default);

        Assert.Null(result.Value!.Logo);
        Assert.Null(result.Value.LogoContentType);
        Assert.Equal("Centre", result.Value.Name);
    }

    [Fact]
    [Trait("spec", Spec + ": Logo del centro; Color de acento")]
    public async Task Keeping_the_logo_changes_the_rest_only()
    {
        await Save.HandleAsync(new SaveCentreIdentityRequest("Centre", LogoChange.Replace, _png, null), default);

        var result = await Save.HandleAsync(new SaveCentreIdentityRequest("Centre nou", LogoChange.Keep, null, "#88aacc"), default);

        Assert.Equal(_png, result.Value!.Logo);
        Assert.Equal("#88AACC", result.Value.Accent);
    }

    [Theory]
    [Trait("spec", Spec + ": Color de acento (Elegir un acento, Restablecer)")]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public async Task An_accent_that_is_not_rrggbb_is_refused(string accent)
    {
        var result = await Save.HandleAsync(new SaveCentreIdentityRequest("Centre", LogoChange.Keep, null, accent), default);

        Assert.Equal("Identity.AccentInvalid", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Color de acento (Restablecer)")]
    public async Task Resetting_the_accent_saves_none_so_the_default_is_used()
    {
        await Save.HandleAsync(new SaveCentreIdentityRequest("Centre", LogoChange.Keep, null, "#88AACC"), default);

        var result = await Save.HandleAsync(new SaveCentreIdentityRequest("Centre", LogoChange.Keep, null, null), default);

        Assert.Null(result.Value!.Accent);
    }
}
