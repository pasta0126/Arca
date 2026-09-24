// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;
using Xunit;

namespace Arca.Domain.Tests.Common;

public sealed class TextComparerTests
{
    [Theory]
    [Trait("spec", "arquitectura-base/internacionalitzacio: Ordenación y búsqueda según el catalán")]
    [InlineData("García", "garcia")]
    [InlineData("Núria", "NURIA")]
    [InlineData("Àngels", "angels")]
    [InlineData("Çaragol", "caragol")]
    public void Search_ignores_case_and_accents(string stored, string typed)
    {
        Assert.True(TextComparer.SearchEquals(stored, typed));
        Assert.True(TextComparer.Contains(stored, typed));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/internacionalitzacio: Ordenación y búsqueda según el catalán")]
    public void Search_finds_a_surname_inside_a_full_name()
    {
        Assert.True(TextComparer.Contains("Sebastià Garcia Puig", "garcía"));
        Assert.True(TextComparer.StartsWith("García Puig", "gar"));
        Assert.False(TextComparer.Contains("Sebastià Puig", "garcia"));
    }

    [Fact]
    public void Empty_query_matches_everything()
    {
        Assert.True(TextComparer.Contains("anything", ""));
        Assert.True(TextComparer.Contains("anything", null));
        Assert.True(TextComparer.StartsWith("anything", ""));
    }

    [Fact]
    public void Null_text_never_matches_a_query()
    {
        Assert.False(TextComparer.Contains(null, "a"));
        Assert.False(TextComparer.SearchEquals(null, "a"));
    }

    [Fact]
    [Trait("spec", "arquitectura-base/internacionalitzacio: Ordenación y búsqueda según el catalán")]
    public void C_cedilla_sorts_with_the_c_words()
    {
        var sorted = new[] { "Zoe", "Colomer", "Çaragol", "Cabanes" }.OrderBy(x => x, TextComparer.Comparer).ToArray();

        Assert.Equal(["Cabanes", "Çaragol", "Colomer", "Zoe"], sorted);
    }

    [Fact]
    [Trait("spec", "arquitectura-base/internacionalitzacio: Ordenación y búsqueda según el catalán")]
    public void Geminated_l_and_ny_sort_in_their_place()
    {
        var sorted = new[] { "Ordeix", "Llop", "Nyerro", "L·l", "Ll", "Nuria", "Nadal" }
            .OrderBy(x => x, TextComparer.Comparer).ToArray();

        Assert.Equal(["Ll", "L·l", "Llop", "Nadal", "Nuria", "Nyerro", "Ordeix"], sorted);
    }

    [Fact]
    [Trait("spec", "arquitectura-base/internacionalitzacio: Ordenación y búsqueda según el catalán")]
    public void Ordering_ignores_case()
    {
        Assert.Equal(0, TextComparer.Compare("garcia", "GARCIA"));
    }

    [Fact]
    public void Ordering_uses_catalan_rules_regardless_of_current_culture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("sv-SE");
            var sorted = new[] { "Zoe", "Çaragol", "Cabanes" }.OrderBy(x => x, TextComparer.Comparer).ToArray();
            Assert.Equal(["Cabanes", "Çaragol", "Zoe"], sorted);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Icu_is_active_so_catalan_rules_are_available()
    {
        // In invariant-globalization mode accents would not be ignored and Catalan rules would not apply.
        Assert.True(TextComparer.SearchEquals("é", "e"));
    }
}
