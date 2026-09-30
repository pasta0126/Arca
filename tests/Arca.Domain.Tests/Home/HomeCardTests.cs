// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Home;
using Xunit;

namespace Arca.Domain.Tests.Home;

public sealed class HomeCardTests
{
    const string Spec = "filtres-i-targetes/targetes-d-inici: Una tarjeta es un filtro guardado";

    static Dictionary<string, string> Criteria(params (string Name, string Value)[] items) => items.ToDictionary(i => i.Name, i => i.Value);

    [Fact]
    [Trait("spec", Spec + " (Tarjeta de taquillas libres)")]
    public void A_card_keeps_its_title_its_screen_and_its_criteria_and_nothing_more()
    {
        var zone = Guid.NewGuid();

        var card = HomeCard.Create(Guid.NewGuid(), "  Taquilles lliures  ", HomeCardTarget.LockerMap, Criteria(("Status", "Free"), ("Zone", zone.ToString())), 3, "lockers-free").Value!;

        Assert.Equal(("Taquilles lliures", HomeCardTarget.LockerMap, 3, "lockers-free"), (card.Title, card.Target, card.Position, card.SeedKey));
        Assert.Equal(new Dictionary<string, string> { ["Status"] = "Free", ["Zone"] = zone.ToString() }, card.Criteria);
    }

    [Fact]
    [Trait("spec", Spec + " (Criterios combinados)")]
    public void The_criteria_of_the_students_combine_and_are_kept_in_a_stable_order()
    {
        var card = HomeCard.Create(Guid.NewGuid(), "Sense taquilla de 2n", HomeCardTarget.Students, Criteria(("Level", "2n ESO"), ("Locker", "without"), ("IncludeRetired", "true")), 0).Value!;

        Assert.Equal("{\"IncludeRetired\":\"true\",\"Level\":\"2n ESO\",\"Locker\":\"without\"}", card.CriteriaText);
        Assert.Equal(3, card.Criteria.Count);
    }

    [Theory]
    [InlineData("Status", "Sleeping")]
    [InlineData("Status", "")]
    [InlineData("Zone", "not-a-guid")]
    [InlineData("Locker", "maybe")]
    [InlineData("Payment", "owes")]
    [InlineData("IncludeRetired", "false")]
    [InlineData("Level", "")]
    [Trait("spec", Spec)]
    public void A_value_a_screen_does_not_understand_is_refused_with_the_name_of_the_criterion(string name, string value)
    {
        var target = name is "Status" or "Zone" ? HomeCardTarget.Lockers : HomeCardTarget.Students;

        var result = HomeCard.Create(Guid.NewGuid(), "Prova", target, Criteria((name, value)), 0);

        Assert.Equal("HomeCards.CriterionInvalid", result.Error!.Code);
        Assert.Equal(name, result.Error.Args![0]);
    }

    [Theory]
    [InlineData(HomeCardTarget.Students, "Status", "Free")]
    [InlineData(HomeCardTarget.Lockers, "Payment", "pending")]
    [InlineData(HomeCardTarget.LockerMap, "Level", "1r ESO")]
    [Trait("spec", Spec)]
    public void A_criterion_the_screen_does_not_have_is_refused(HomeCardTarget target, string name, string value)
    {
        var result = HomeCard.Create(Guid.NewGuid(), "Prova", target, Criteria((name, value)), 0);

        Assert.Equal("HomeCards.CriterionUnknown", result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Crear una tarjeta desde una pantalla filtrada (Título vacío)")]
    public void An_empty_title_is_refused(string? title)
    {
        Assert.Equal("HomeCards.TitleRequired", HomeCard.Create(Guid.NewGuid(), title, HomeCardTarget.Students, null, 0).Error!.Code);
    }

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Crear una tarjeta desde una pantalla filtrada (Título vacío)")]
    public void A_title_of_up_to_60_characters_is_kept_and_a_longer_one_is_refused()
    {
        Assert.True(HomeCard.CheckTitle(new string('a', 60)).IsSuccess);

        var refused = HomeCard.CheckTitle(new string('a', 61));

        Assert.Equal("HomeCards.TitleTooLong", refused.Error!.Code);
        Assert.Equal(60, refused.Error.Args![0]);
    }

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Editar, ordenar y borrar tarjetas (Editar)")]
    public void Editing_changes_the_title_the_screen_and_the_criteria_together_and_a_refusal_changes_nothing()
    {
        var card = HomeCard.Create(Guid.NewGuid(), "Lliures", HomeCardTarget.LockerMap, Criteria(("Status", "Free")), 0).Value!;

        var refused = card.Edit("Canviat", HomeCardTarget.Students, Criteria(("Status", "Free"))); // a status is not a student criterion
        Assert.False(refused.IsSuccess);
        Assert.Equal(("Lliures", HomeCardTarget.LockerMap), (card.Title, card.Target));

        Assert.True(card.Edit("Avariades", HomeCardTarget.Lockers, Criteria(("Status", "Broken"))).IsSuccess);
        Assert.Equal(("Avariades", HomeCardTarget.Lockers, "Broken"), (card.Title, card.Target, card.Criteria["Status"]));
    }

    [Fact]
    [Trait("spec", "filtres-i-targetes/targetes-d-inici: Editar, ordenar y borrar tarjetas (Mover)")]
    public void A_card_moves_to_another_place()
    {
        var card = HomeCard.Create(Guid.NewGuid(), "Lliures", HomeCardTarget.LockerMap, null, 4).Value!;

        card.MoveTo(1);

        Assert.Equal(1, card.Position);
    }

    [Fact]
    [Trait("spec", Spec + " (Sin datos personales)")]
    public void Stored_criteria_that_cannot_be_read_mean_no_filter_and_never_an_error()
    {
        Assert.Empty(HomeCardCriteria.Deserialize("not json"));
        Assert.Empty(HomeCardCriteria.Deserialize(null));
        Assert.Empty(HomeCardCriteria.Deserialize("  "));
    }

    [Fact]
    [Trait("spec", Spec + " (Sin datos personales)")]
    public void Nothing_in_a_card_can_hold_the_name_email_or_identifier_of_a_student()
    {
        var names = typeof(HomeCardCriteria).GetFields().Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()!);
        var forbidden = new[] { "Email", "Dni", "Identifier", "Name", "Student" };

        Assert.DoesNotContain(names, n => forbidden.Any(f => n.Contains(f, StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(typeof(HomeCard).GetProperties(), p => forbidden.Any(f => p.Name.Contains(f, StringComparison.OrdinalIgnoreCase)));
    }
}
