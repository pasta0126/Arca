// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Lockers;
using Xunit;

namespace Arca.Domain.Tests.Lockers;

public sealed class LockerStatusTests
{
    const string Spec = "taquilles-i-zones/taquilles: Estado visible derivado";

    // Not retired: out of service, has assignment, is reserved -> expected. All twelve combinations, written out.
    public static TheoryData<OutOfServiceKind?, bool, bool, LockerStatus> ActiveTable => new()
    {
        { null, false, false, LockerStatus.Free },
        { null, false, true, LockerStatus.Reserved },
        { null, true, false, LockerStatus.Occupied },
        { null, true, true, LockerStatus.Occupied },
        { OutOfServiceKind.Broken, false, false, LockerStatus.Broken },
        { OutOfServiceKind.Broken, false, true, LockerStatus.Broken },
        { OutOfServiceKind.Broken, true, false, LockerStatus.Broken },
        { OutOfServiceKind.Broken, true, true, LockerStatus.Broken },
        { OutOfServiceKind.Maintenance, false, false, LockerStatus.Maintenance },
        { OutOfServiceKind.Maintenance, false, true, LockerStatus.Maintenance },
        { OutOfServiceKind.Maintenance, true, false, LockerStatus.Maintenance },
        { OutOfServiceKind.Maintenance, true, true, LockerStatus.Maintenance },
    };

    [Theory]
    [MemberData(nameof(ActiveTable))]
    [Trait("spec", Spec + " (precedencia: fuera de servicio, ocupada, reservada, libre)")]
    public void The_status_of_a_locker_that_is_not_retired_follows_the_precedence(
        OutOfServiceKind? outOfService, bool hasAssignment, bool isReserved, LockerStatus expected)
    {
        var state = LockerStatusCalculator.Calculate(new LockerFacts(false, outOfService, hasAssignment, isReserved));

        Assert.Equal(expected, state.Status);
        Assert.Equal(hasAssignment, state.HasAssignment);
        Assert.Equal(isReserved, state.IsReserved);
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData(null, true, false)]
    [InlineData(null, false, true)]
    [InlineData(null, true, true)]
    [InlineData(OutOfServiceKind.Broken, false, false)]
    [InlineData(OutOfServiceKind.Broken, true, true)]
    [InlineData(OutOfServiceKind.Maintenance, false, true)]
    [InlineData(OutOfServiceKind.Maintenance, true, false)]
    [Trait("spec", Spec + " (precedencia: de baja)")]
    public void A_retired_locker_is_retired_whatever_else_it_has(OutOfServiceKind? outOfService, bool hasAssignment, bool isReserved)
    {
        var state = LockerStatusCalculator.Calculate(new LockerFacts(true, outOfService, hasAssignment, isReserved));

        Assert.Equal(LockerStatus.Retired, state.Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquilla sin hechos")]
    public void A_locker_with_no_facts_is_free()
    {
        Assert.Equal(LockerStatus.Free, LockerStatusCalculator.Calculate(default).Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquilla averiada con alumno")]
    public void A_broken_locker_shows_broken_and_says_it_keeps_the_assignment()
    {
        var state = LockerStatusCalculator.Calculate(new LockerFacts(false, OutOfServiceKind.Broken, true, false));

        Assert.Equal(LockerStatus.Broken, state.Status);
        Assert.True(state.HasAssignment);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquilla reservada y averiada")]
    public void A_reserved_locker_that_breaks_shows_broken_and_keeps_the_reservation()
    {
        var state = LockerStatusCalculator.Calculate(new LockerFacts(false, OutOfServiceKind.Broken, false, true));

        Assert.Equal(LockerStatus.Broken, state.Status);
        Assert.True(state.IsReserved);
    }

    [Fact]
    [Trait("spec", Spec + ": Estado tras resolver la avería")]
    public void Resolving_the_breakdown_of_an_occupied_locker_shows_it_occupied_again_with_no_other_action()
    {
        var broken = new LockerFacts(false, OutOfServiceKind.Broken, true, false);
        var resolved = broken with { OutOfService = null };

        Assert.Equal(LockerStatus.Broken, LockerStatusCalculator.Calculate(broken).Status);
        Assert.Equal(LockerStatus.Occupied, LockerStatusCalculator.Calculate(resolved).Status);
    }

    [Fact]
    [Trait("spec", Spec + ": Taquilla reservada y averiada")]
    public void Resolving_the_breakdown_of_a_reserved_locker_shows_it_reserved_again()
    {
        var resolved = new LockerFacts(false, null, false, true);

        Assert.Equal(LockerStatus.Reserved, LockerStatusCalculator.Calculate(resolved).Status);
    }
}
