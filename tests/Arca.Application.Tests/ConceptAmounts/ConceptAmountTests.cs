// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Application.Tests.ConceptAmounts;

public sealed class ConceptAmountTests
{
    const string Spec = "pagaments/conceptes-de-cobrament";

    [Fact]
    [Trait("spec", Spec + ": Importe por curso (Definir importes de un curso)")]
    public async Task Defining_the_three_amounts_saves_them_for_the_year()
    {
        var world = new ConceptAmountsWorld();
        var year = await world.YearAsync(2026);

        var result = (await world.SetAsync(year, 50m, 20m, 10m)).Value!;

        Assert.Equal(50m, result.Fee);
        Assert.Equal(20m, result.Deposit);
        Assert.Equal(10m, result.KeyReplacementFee);
        Assert.True(result.IsEditable);
        Assert.False(result.IsProposed);
    }

    [Theory]
    [Trait("spec", Spec + ": Importe por curso (Importe no válido)")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.999)]
    [InlineData(10000)]
    public async Task An_invalid_amount_is_refused(double invalid)
    {
        var world = new ConceptAmountsWorld();
        var year = await world.YearAsync(2026);

        var result = await world.SetAsync(year, (decimal)invalid, 20m, 10m);

        Assert.Equal("ConceptAmounts.AmountInvalid", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Importes iniciales heredados (Curso con antecesor)")]
    public async Task A_year_with_no_amounts_yet_proposes_the_previous_years_amounts()
    {
        var world = new ConceptAmountsWorld();
        var first = await world.YearAsync(2024);
        await world.SeedAsync(first, 45m, 18m, 9m);
        var second = await world.YearAsync(2026);

        var proposed = (await world.Get.HandleAsync(new(second), default)).Value!;

        Assert.Equal(45m, proposed.Fee);
        Assert.Equal(18m, proposed.Deposit);
        Assert.Equal(9m, proposed.KeyReplacementFee);
        Assert.True(proposed.IsProposed);
    }

    [Fact]
    [Trait("spec", Spec + ": Importes iniciales heredados (Primer curso)")]
    public async Task A_previous_year_with_no_amounts_of_its_own_proposes_nothing_either()
    {
        var world = new ConceptAmountsWorld();
        await world.YearAsync(2024); // exists, but its own amounts were never defined
        var year = await world.YearAsync(2026);

        var view = (await world.Get.HandleAsync(new(year), default)).Value!;

        Assert.Null(view.Fee);
        Assert.False(view.IsProposed);
    }

    [Fact]
    [Trait("spec", Spec + ": Importes iniciales heredados (Primer curso)")]
    public async Task The_first_year_of_all_has_empty_fields_with_nothing_proposed()
    {
        var world = new ConceptAmountsWorld();
        var year = await world.YearAsync(2026);

        var view = (await world.Get.HandleAsync(new(year), default)).Value!;

        Assert.Null(view.Fee);
        Assert.Null(view.Deposit);
        Assert.Null(view.KeyReplacementFee);
        Assert.False(view.IsProposed);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de importe sin efecto retroactivo (Subida de la cuota)")]
    public async Task Changing_the_fee_replaces_the_amount_used_for_charges_generated_from_now_on()
    {
        // Whether a charge already generated keeps its old amount is a guarantee of the charge itself (Charge.Amount is
        // fixed when the charge is created, D1); it is proven end to end once charges exist, in group 3. Here only the
        // amount that future charges would read is checked.
        var world = new ConceptAmountsWorld();
        var year = await world.YearAsync(2026);
        await world.SetAsync(year, 50m, 20m, 10m);

        var changed = (await world.SetAsync(year, 55m, 20m, 10m)).Value!;

        Assert.Equal(55m, changed.Fee);
        Assert.Equal(20m, changed.Deposit);
        Assert.Equal(10m, changed.KeyReplacementFee);
    }

    [Fact]
    [Trait("spec", Spec + ": Cambio de importe sin efecto retroactivo (Cambio registrado)")]
    public async Task Changing_an_amount_keeps_the_before_and_after_value_in_the_history()
    {
        var world = new ConceptAmountsWorld();
        var year = await world.YearAsync(2026);
        await world.SetAsync(year, 50m, 20m, 10m);
        var fee = (await world.Store.ConceptAmounts.ListByYearAsync(year, default)).Single(a => a.Concept == ChargeConcept.Fee);

        await world.SetAsync(year, 55m, 20m, 10m);

        var history = await world.Store.ConceptAmountEvents.ListAsync(fee.Id, default);
        Assert.Equal([ConceptAmountEventTypes.Changed, ConceptAmountEventTypes.Created], history.Select(e => e.Type));
        Assert.Contains("50", history[0].BeforeJson, StringComparison.Ordinal);
        Assert.Contains("55", history[0].AfterJson, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("spec", Spec + ": Importes de cursos finalizados (Consultar un curso finalizado)")]
    public async Task A_finished_year_can_be_read_but_not_changed()
    {
        var world = new ConceptAmountsWorld();
        var year = await world.YearAsync(2020);
        await world.SeedAsync(year, 50m, 20m, 10m);

        var view = (await world.Get.HandleAsync(new(year), default)).Value!;

        Assert.Equal(50m, view.Fee);
        Assert.False(view.IsEditable);
    }

    [Fact]
    [Trait("spec", Spec + ": Importes de cursos finalizados (Modificar un curso finalizado)")]
    public async Task Changing_the_amounts_of_a_finished_year_is_refused()
    {
        var world = new ConceptAmountsWorld();
        var year = await world.YearAsync(2020);

        var result = await world.SetAsync(year, 50m, 20m, 10m);

        Assert.Equal("ConceptAmounts.YearFinished", result.Error!.Code);
    }
}
