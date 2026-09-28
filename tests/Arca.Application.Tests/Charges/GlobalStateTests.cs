// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.VoidCharge;
using Arca.Application.GlobalState;
using Arca.Domain.ConceptAmounts;
using Xunit;

namespace Arca.Application.Tests.Charges;

public sealed class GlobalStateTests
{
    const string Spec = "ui-shell/navegacio-i-cerca";

    static GetGlobalStateHandler Handler(PagamentsWorld world) => new(world.Store.Years, world.Store.Charges);

    [Fact]
    [Trait("spec", Spec + ": Cabecera con el estado global (Curso activo)")]
    public async Task The_state_names_the_active_year()
    {
        var world = new PagamentsWorld();
        await world.YearAsync(2026);

        var state = (await Handler(world).HandleAsync(default)).Value!;

        Assert.True(state.HasActiveYear);
        Assert.Equal("2026-2027", state.ActiveYear!.Name);
    }

    [Fact]
    [Trait("spec", Spec + ": Avisos globales (Sin curso activo)")]
    public async Task Without_an_active_year_the_state_says_so()
    {
        var world = new PagamentsWorld();

        var state = (await Handler(world).HandleAsync(default)).Value!;

        Assert.False(state.HasActiveYear);
        Assert.Null(state.ActiveYear);
        Assert.Equal(0, state.PendingCharges);
    }

    [Fact]
    [Trait("spec", Spec + ": Indicadores de sección (Nada que atender)")]
    public async Task Only_pending_charges_of_any_year_count_as_needing_attention()
    {
        var world = new PagamentsWorld();
        var current = await world.YearAsync(2026);
        var previous = await world.YearAsync(2025);
        await world.SeedAmountsAsync(current, 50m, 20m, 10m);
        var zone = await world.ZoneAsync("Planta 1");
        var student = await world.StudentAsync("Marta", "Puig", "marta@example.com");
        await world.AssignAsync(student.Id, await world.LockerAsync(1, zone)); // a pending fee and a pending deposit
        var old = await world.SeedChargeAsync(student.Id, ChargeConcept.KeyReplacementFee, previous, 50m); // and one of an earlier year
        Assert.Equal(3, (await Handler(world).HandleAsync(default)).Value!.PendingCharges);

        var fee = world.ChargesOf(student.Id).First(c => c.Concept == ChargeConcept.Fee);
        await world.MarkPaid.HandleAsync(new MarkChargePaidRequest(fee.Id, null), default);
        await world.Void.HandleAsync(new VoidChargeRequest(old.Id, "Error"), default);

        Assert.Equal(1, (await Handler(world).HandleAsync(default)).Value!.PendingCharges); // only the deposit is left
    }
}
