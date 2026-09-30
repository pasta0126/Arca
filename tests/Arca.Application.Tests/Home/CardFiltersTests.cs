// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Home;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Search;
using Arca.Application.Students.ListStudentRows;
using Xunit;

namespace Arca.Application.Tests.Home;

public sealed class CardFiltersTests
{
    const string Spec = "filtres-i-targetes/targetes-d-inici: Una tarjeta es un filtro guardado";

    static readonly Guid _zoneOne = Guid.NewGuid();
    static readonly Guid _zoneTwo = Guid.NewGuid();

    static LockerListRow Locker(int number, Guid zone, LockerStatusView status) => new(Guid.NewGuid(), number, zone, zone == _zoneOne ? "Planta 1" : "Planta 2", status, null, false, null, null);

    static StudentListRow Student(string last, string level, string group, int? locker, bool debt, bool retired = false) =>
        new(Guid.NewGuid(), "Nom", last, level, group, retired, locker, debt, debt ? 50m : 0m);

    static readonly LockerListRow[] _lockers =
    [
        Locker(1, _zoneOne, LockerStatusView.Free), Locker(2, _zoneOne, LockerStatusView.Occupied), Locker(3, _zoneTwo, LockerStatusView.Free),
        Locker(4, _zoneTwo, LockerStatusView.Broken), Locker(5, _zoneTwo, LockerStatusView.Retired),
    ];

    static readonly StudentListRow[] _students =
    [
        Student("Puig", "1r ESO", "A", 5, false), Student("Alsina", "2n ESO", "B", null, true), Student("Roca", "2n ESO", "A", null, false),
        Student("Zamora", "1r ESO", "B", null, true, retired: true), Student("Font", "2n ESO", "B", 9, true),
    ];

    [Fact]
    [Trait("spec", Spec + " (Tarjeta de taquillas libres)")]
    public void The_status_and_the_zone_keep_only_the_active_lockers_that_have_both()
    {
        var free = new LockerCardFilter(LockerStatusView.Free);
        var freeInTwo = new LockerCardFilter(LockerStatusView.Free, _zoneTwo);

        Assert.Equal([1, 3], _lockers.Where(free.Matches).Select(l => l.Number));
        Assert.Equal([3], _lockers.Where(freeInTwo.Matches).Select(l => l.Number));
        Assert.Equal([1, 2, 3, 4], _lockers.Where(new LockerCardFilter().Matches).Select(l => l.Number)); // no filter: every active one, never a retired one
    }

    [Fact]
    [Trait("spec", Spec + " (Tarjeta de taquillas libres)")]
    public void A_list_that_shows_the_retired_uses_the_criteria_alone()
    {
        var retired = new LockerCardFilter(LockerStatusView.Retired);

        Assert.Equal([5], _lockers.Where(retired.MatchesCriteria).Select(l => l.Number));
        Assert.DoesNotContain(_lockers, l => retired.Matches(l));
    }

    [Theory]
    [InlineData("without", null, null, null, false, "Alsina,Roca")]
    [InlineData("with", null, null, null, false, "Font,Puig")]
    [InlineData(null, "pending", null, null, false, "Alsina,Font,Zamora")] // the one who left still owes
    [InlineData(null, "upToDate", null, null, false, "Puig,Roca")]
    [InlineData("without", null, "2n ESO", null, false, "Alsina,Roca")]
    [InlineData("without", "pending", "2n ESO", "B", false, "Alsina")]
    [InlineData(null, null, null, null, true, "Alsina,Font,Puig,Roca,Zamora")]
    [InlineData("without", null, null, null, true, "Alsina,Roca")] // whoever left has no locker to be without
    [Trait("spec", Spec + " (Criterios combinados)")]
    public void The_criteria_of_the_students_combine(string? locker, string? payment, string? level, string? group, bool retired, string expected)
    {
        var filter = new StudentCardFilter(locker, payment, level, group, retired);

        Assert.Equal(expected.Split(','), _students.Where(filter.Matches).Select(s => s.LastName).Order(StringComparer.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec + " (Criterios combinados)")]
    public void Without_any_criterion_only_the_active_students_are_kept()
    {
        Assert.Equal(["Alsina", "Font", "Puig", "Roca"], _students.Where(new StudentCardFilter().Matches).Select(s => s.LastName).Order(StringComparer.Ordinal));
    }

    [Fact]
    [Trait("spec", Spec)]
    public void A_filter_is_written_as_criteria_and_read_back_the_same_leaving_out_what_does_not_filter()
    {
        var students = new StudentCardFilter("without", "pending", "2n ESO", null, true);
        var lockers = new LockerCardFilter(LockerStatusView.Broken, _zoneTwo);

        Assert.Equal(students, StudentCardFilter.From(students.ToCriteria()));
        Assert.Equal(lockers, LockerCardFilter.From(lockers.ToCriteria()));
        Assert.Empty(new StudentCardFilter().ToCriteria());
        Assert.Empty(new LockerCardFilter().ToCriteria());
        Assert.Equal(["Locker", "Payment", "Level", "IncludeRetired"], students.ToCriteria().Keys);
    }

    [Fact]
    [Trait("spec", Spec)]
    public void What_is_not_understood_does_not_filter()
    {
        var criteria = new Dictionary<string, string> { ["Status"] = "Sleeping", ["Zone"] = "nowhere" };

        Assert.Equal(new LockerCardFilter(), LockerCardFilter.From(criteria));
        Assert.Equal(new StudentCardFilter(), StudentCardFilter.From(new Dictionary<string, string> { ["Level"] = string.Empty }));
    }
}
