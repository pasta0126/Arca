// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Arca.Domain.Zones;
using Xunit;

namespace Arca.Domain.Tests.Zones;

public sealed class ZoneTests
{
    const string Spec = "taquilles-i-zones/zones";

    static Zone Make(string name, bool active = true)
    {
        var zone = Zone.Create(Guid.NewGuid(), name, []).Value!;
        if (!active)
        {
            zone.Deactivate(0);
        }

        return zone;
    }

    [Fact]
    [Trait("spec", Spec + ": Crear zonas (Zona nueva)")]
    public void A_new_zone_is_created_active()
    {
        var result = Zone.Create(Guid.NewGuid(), "Planta 1", []);

        Assert.True(result.IsSuccess);
        Assert.Equal("Planta 1", result.Value!.Name);
        Assert.True(result.Value.IsActive);
    }

    [Theory]
    [Trait("spec", Spec + ": Crear zonas (Nombre vacío)")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_name_is_refused(string? name)
    {
        var result = Zone.Create(Guid.NewGuid(), name, []);

        Assert.Equal("Zones.NameRequired", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Crear zonas (Nombre demasiado largo)")]
    public void A_name_of_more_than_60_characters_is_refused_and_60_is_accepted()
    {
        var tooLong = Zone.Create(Guid.NewGuid(), new string('a', 61), []);
        var exact = Zone.Create(Guid.NewGuid(), new string('a', 60), []);

        Assert.Equal("Zones.NameTooLong", tooLong.Error!.Code);
        Assert.Equal(60, Assert.Single(tooLong.Error.Args));
        Assert.True(exact.IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Crear zonas (Espacios sobrantes)")]
    public void Surrounding_spaces_are_trimmed_before_the_length_is_checked()
    {
        var result = Zone.Create(Guid.NewGuid(), "  Gimnasio  ", []);
        var padded = Zone.Create(Guid.NewGuid(), "  " + new string('a', 60) + "  ", []);

        Assert.Equal("Gimnasio", result.Value!.Name);
        Assert.True(padded.IsSuccess);
    }

    [Theory]
    [Trait("spec", Spec + ": Nombre de zona único (Nombre duplicado)")]
    [InlineData("gimnas")]
    [InlineData("GIMNÀS")]
    [InlineData("  Gimnàs ")]
    [InlineData("Gimnàs")]
    public void A_name_that_differs_only_in_case_accents_or_spaces_is_a_duplicate(string typed)
    {
        var existing = Make("Gimnàs");

        var result = Zone.Create(Guid.NewGuid(), typed, [existing]);

        Assert.Equal("Zones.NameDuplicate", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Nombre de zona único (Nombre duplicado)")]
    public void Spaces_inside_the_name_do_not_make_it_different()
    {
        var existing = Make("Planta  1");

        Assert.Equal("Zones.NameDuplicate", Zone.Create(Guid.NewGuid(), "planta 1", [existing]).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Nombre de zona único (Nombre de una zona desactivada)")]
    public void The_name_of_a_deactivated_zone_is_still_taken()
    {
        var inactive = Make("Planta 2", active: false);

        Assert.False(inactive.IsActive);
        Assert.Equal("Zones.NameDuplicate", Zone.Create(Guid.NewGuid(), "Planta 2", [inactive]).Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Renombrar zonas (Renombrado correcto)")]
    public void Renaming_changes_the_name_and_keeps_the_identity()
    {
        var zone = Make("Planta 1");
        var id = zone.Id;

        var result = zone.Rename("Primera planta", [zone]);

        Assert.True(result.IsSuccess);
        Assert.Equal("Primera planta", zone.Name);
        Assert.Equal(id, zone.Id);
    }

    [Theory]
    [Trait("spec", Spec + ": Renombrar zonas (Renombrar con el mismo nombre)")]
    [InlineData("planta 1")]
    [InlineData("PLANTA 1")]
    [InlineData("Planta 1")]
    public void Renaming_a_zone_to_its_own_name_changing_only_case_is_accepted(string typed)
    {
        var zone = Make("Planta 1");

        var result = zone.Rename(typed, [zone]);

        Assert.True(result.IsSuccess);
        Assert.Equal(typed, zone.Name);
    }

    [Fact]
    [Trait("spec", Spec + ": Renombrar zonas (Renombrar con el mismo nombre)")]
    public void Renaming_a_zone_to_its_own_name_changing_only_accents_is_accepted()
    {
        var zone = Make("Gimnas");

        Assert.True(zone.Rename("Gimnàs", [zone]).IsSuccess);
        Assert.Equal("Gimnàs", zone.Name);
    }

    [Fact]
    [Trait("spec", Spec + ": Renombrar zonas (Renombrar a un nombre existente)")]
    public void Renaming_to_the_name_of_another_zone_is_refused_and_changes_nothing()
    {
        var zone = Make("Planta 1");
        var other = Make("Gimnàs");

        var result = zone.Rename("gimnas", [zone, other]);

        Assert.Equal("Zones.NameDuplicate", result.Error!.Code);
        Assert.Equal("Planta 1", zone.Name);
    }

    [Fact]
    [Trait("spec", Spec + ": Renombrar zonas (Renombrado correcto)")]
    public void Renaming_with_an_invalid_name_is_refused_and_changes_nothing()
    {
        var zone = Make("Planta 1");

        Assert.Equal("Zones.NameRequired", zone.Rename(" ", [zone]).Error!.Code);
        Assert.Equal("Zones.NameTooLong", zone.Rename(new string('x', 61), [zone]).Error!.Code);
        Assert.Equal("Planta 1", zone.Name);
    }

    [Fact]
    [Trait("spec", Spec + ": Desactivar y reactivar zonas (Desactivar zona sin taquillas activas)")]
    public void A_zone_without_active_lockers_can_be_deactivated()
    {
        var zone = Make("Planta 1");

        var result = zone.Deactivate(activeLockers: 0);

        Assert.True(result.IsSuccess);
        Assert.False(zone.IsActive);
    }

    [Theory]
    [Trait("spec", Spec + ": Desactivar y reactivar zonas (Desactivar zona con taquillas activas)")]
    [InlineData(1)]
    [InlineData(40)]
    public void A_zone_with_active_lockers_cannot_be_deactivated_and_the_error_says_how_many(int activeLockers)
    {
        var zone = Make("Planta 1");

        var result = zone.Deactivate(activeLockers);

        Assert.Equal("Zones.HasActiveLockers", result.Error!.Code);
        Assert.Equal(activeLockers, Assert.Single(result.Error.Args));
        Assert.True(zone.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Desactivar y reactivar zonas (Reactivar)")]
    public void A_deactivated_zone_can_be_reactivated()
    {
        var zone = Make("Planta 1", active: false);

        var result = zone.Reactivate([zone]);

        Assert.True(result.IsSuccess);
        Assert.True(zone.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Desactivar y reactivar zonas (Reactivar)")]
    public void Reactivating_is_refused_if_another_zone_has_taken_the_name()
    {
        var zone = new Zone(Guid.NewGuid(), "Planta 1", TextComparer.Key("Planta 1"), isActive: false);
        var other = new Zone(Guid.NewGuid(), "planta 1", TextComparer.Key("planta 1"), isActive: true); // as if stored by hand

        var result = zone.Reactivate([zone, other]);

        Assert.Equal("Zones.NameDuplicate", result.Error!.Code);
        Assert.False(zone.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Conservación de zonas con historial (Eliminar zona sin uso)")]
    public void A_zone_that_never_had_lockers_can_be_deleted()
    {
        Assert.True(Make("Nova").CheckCanDelete(everHadLockers: false).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + ": Conservación de zonas con historial (Eliminar zona con historial)")]
    public void A_zone_that_has_or_had_lockers_cannot_be_deleted()
    {
        var result = Make("Planta 1").CheckCanDelete(everHadLockers: true);

        Assert.Equal("Zones.HasHistory", result.Error!.Code);
    }

    [Theory]
    [Trait("spec", Spec + ": Nombre de zona único (Nombre duplicado)")]
    [InlineData("Gimnàs", "gimnas", true)]
    [InlineData("Ça", "CA", true)]
    [InlineData("Planta   1", "planta 1", true)]
    [InlineData("Planta 1", "Planta 2", false)]
    [InlineData("l·l", "ll", false)]
    public void The_name_key_ignores_case_accents_and_repeated_spaces_only(string a, string b, bool same)
    {
        Assert.Equal(same, TextComparer.Key(a) == TextComparer.Key(b));
    }
}
