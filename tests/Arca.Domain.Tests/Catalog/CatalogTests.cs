// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Catalog;
using Xunit;

namespace Arca.Domain.Tests.Catalog;

public sealed class CatalogTests
{
    const string Spec = "alumnes-i-assignacions/alumnes: Catálogo de niveles y grupos";

    static Level NewLevel(string name, params Level[] existing) => Level.Create(Guid.NewGuid(), name, existing).Value!;

    [Fact]
    [Trait("spec", Spec + " (Valor nuevo en un alta manual)")]
    public void A_new_level_and_a_new_group_are_created_from_what_is_typed()
    {
        var level = Level.Create(Guid.NewGuid(), "  1r ESO ", []);
        var group = Group.Create(Guid.NewGuid(), level.Value!, " A", []);

        Assert.Equal("1r ESO", level.Value!.Name);
        Assert.Equal("A", group.Value!.Name);
        Assert.Equal(level.Value.Id, group.Value.LevelId);
    }

    [Theory]
    [InlineData("1R eso")]
    [InlineData("1r  ESO")]
    [InlineData(" 1r eso ")]
    [Trait("spec", Spec + " (Equivalencia de grafías)")]
    public void A_level_that_differs_only_in_case_or_spaces_is_the_same_level(string typed)
    {
        var existing = NewLevel("1r ESO");

        Assert.Equal("Catalog.LevelExists", Level.Create(Guid.NewGuid(), typed, [existing]).Error!.Code);
        Assert.Same(existing, Level.Find(typed, [existing]));
    }

    [Fact]
    [Trait("spec", Spec + " (Equivalencia de grafías)")]
    public void The_group_a_and_the_group_A_of_a_level_are_the_same_group_and_accents_do_not_matter()
    {
        var level = NewLevel("1r ESO");
        var existing = Group.Create(Guid.NewGuid(), level, "A", []).Value!;
        var accented = Group.Create(Guid.NewGuid(), level, "Èxit", []).Value!;

        Assert.Equal("Catalog.GroupExists", Group.Create(Guid.NewGuid(), level, "a", [existing]).Error!.Code);
        Assert.Same(existing, Group.Find(level.Id, "a", [existing, accented]));
        Assert.Same(accented, Group.Find(level.Id, "exit", [existing, accented]));
    }

    [Fact]
    [Trait("spec", Spec + " (Grupo por nivel)")]
    public void The_group_A_of_another_level_is_a_different_group_managed_apart()
    {
        var first = NewLevel("1r ESO");
        var second = NewLevel("2n ESO", first);
        var groupOfFirst = Group.Create(Guid.NewGuid(), first, "A", []).Value!;

        var groupOfSecond = Group.Create(Guid.NewGuid(), second, "A", [groupOfFirst]);

        Assert.True(groupOfSecond.IsSuccess);
        Assert.NotEqual(groupOfFirst.Id, groupOfSecond.Value!.Id);
        Assert.Null(Group.Find(second.Id, "B", [groupOfFirst, groupOfSecond.Value]));
        Assert.Same(groupOfSecond.Value, Group.Find(second.Id, "A", [groupOfFirst, groupOfSecond.Value]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [Trait("spec", Spec + " (Valor nuevo en un alta manual)")]
    public void A_name_is_required_and_a_blank_one_finds_nothing(string? name)
    {
        var level = NewLevel("1r ESO");

        Assert.Equal("Catalog.NameRequired", Level.Create(Guid.NewGuid(), name, []).Error!.Code);
        Assert.Equal("Catalog.NameRequired", Group.Create(Guid.NewGuid(), level, name, []).Error!.Code);
        Assert.Null(Level.Find(name, [level]));
    }

    [Fact]
    [Trait("spec", Spec + " (Valor nuevo en un alta manual)")]
    public void A_name_of_more_than_50_characters_is_refused()
    {
        Assert.Equal("Catalog.NameTooLong", Level.Create(Guid.NewGuid(), new string('n', 51), []).Error!.Code);
        Assert.True(Level.Create(Guid.NewGuid(), new string('n', 50), []).IsSuccess);
    }

    [Fact]
    [Trait("spec", Spec + " (Equivalencia de grafías)")]
    public void There_are_no_fixed_values_in_the_code()
    {
        var catalogue = typeof(Level).Assembly.GetTypes().Where(t => t.Namespace == "Arca.Domain.Catalog").ToList();

        Assert.DoesNotContain(catalogue.SelectMany(t => t.GetFields()), f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.Contains("ESO", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec + " (Equivalencia de grafías)")]
    public void Names_are_ordered_naturally_so_2n_comes_before_10e()
    {
        var names = new[] { "10è", "2n", "1r", "3r", "11è" };

        var ordered = names.OrderBy(n => n, NaturalComparer.Instance).ToList();

        Assert.Equal(["1r", "2n", "3r", "10è", "11è"], ordered);
    }

    [Fact]
    [Trait("spec", Spec + " (Equivalencia de grafías)")]
    public void Natural_order_compares_text_with_catalan_rules_and_handles_equal_and_empty_names()
    {
        var levels = new[] { "2n ESO", "1r ESO", "1r BATX", "2n BATX", "10è", "Ç", "C", "D" };

        var ordered = levels.OrderBy(n => n, NaturalComparer.Instance).ToList();

        Assert.Equal(["1r BATX", "1r ESO", "2n BATX", "2n ESO", "10è", "C", "Ç", "D"], ordered);
        Assert.Equal(0, NaturalComparer.Instance.Compare("A", "a"));
        Assert.True(NaturalComparer.Instance.Compare("", "A") < 0);
        Assert.True(NaturalComparer.Instance.Compare("A1", "A") > 0);
        Assert.True(NaturalComparer.Instance.Compare(null, "A") < 0);
    }
}
