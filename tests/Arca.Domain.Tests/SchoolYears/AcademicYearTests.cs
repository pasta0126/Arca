// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.SchoolYears;
using Xunit;

namespace Arca.Domain.Tests.SchoolYears;

public sealed class AcademicYearTests
{
    const string Spec = "alumnes-i-assignacions/curs-escolar";

    static AcademicYear Year(int startYear, bool active = false) =>
        AcademicYear.Restore(Guid.NewGuid(), new DateOnly(startYear, 9, 1), new DateOnly(startYear + 1, 6, 30), active);

    [Fact]
    [Trait("spec", Spec + ": Crear un curso escolar (Curso nuevo)")]
    public void A_year_from_1_september_2026_to_30_june_2027_is_named_2026_2027()
    {
        var result = AcademicYear.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30), []);

        Assert.True(result.IsSuccess);
        Assert.Equal("2026-2027", result.Value!.Name);
        Assert.Equal(2026, result.Value.StartYear);
        Assert.Equal(new DateOnly(2027, 6, 30), result.Value.EndDate);
    }

    [Theory]
    [InlineData(2026, 9, 1, 2026, 9, 1)]
    [InlineData(2026, 9, 1, 2026, 8, 31)]
    [InlineData(2027, 6, 30, 2026, 9, 1)]
    [Trait("spec", Spec + ": Crear un curso escolar (Fechas incoherentes)")]
    public void An_end_equal_to_or_before_the_start_is_refused(int sy, int sm, int sd, int ey, int em, int ed)
    {
        var result = AcademicYear.Create(Guid.NewGuid(), new DateOnly(sy, sm, sd), new DateOnly(ey, em, ed), []);

        Assert.Equal("SchoolYears.DatesInvalid", result.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Crear un curso escolar (Solapamiento con otro curso)")]
    public void Dates_that_overlap_another_year_are_refused_naming_it()
    {
        var existing = Year(2026);

        var result = AcademicYear.Create(Guid.NewGuid(), new DateOnly(2027, 6, 1), new DateOnly(2027, 12, 31), [existing]);

        Assert.Equal("SchoolYears.Overlaps", result.Error!.Code);
        Assert.Equal("2026-2027", Assert.Single(result.Error.Args));
    }

    [Fact]
    [Trait("spec", Spec + ": Crear un curso escolar (Solapamiento con otro curso)")]
    public void Consecutive_years_that_touch_without_overlapping_are_accepted_and_one_day_of_overlap_is_not()
    {
        var existing = Year(2026); // ends on 30 June 2027

        var next = AcademicYear.Create(Guid.NewGuid(), new DateOnly(2027, 7, 1), new DateOnly(2028, 6, 30), [existing]);
        var overlapping = AcademicYear.Create(Guid.NewGuid(), new DateOnly(2027, 6, 30), new DateOnly(2028, 6, 30), [existing]);

        Assert.True(next.IsSuccess);
        Assert.Equal("SchoolYears.Overlaps", overlapping.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Crear un curso escolar (Nombre duplicado)")]
    public void A_second_year_starting_in_the_same_year_is_refused_as_already_existing()
    {
        var existing = Year(2026);

        var result = AcademicYear.Create(Guid.NewGuid(), new DateOnly(2026, 10, 1), new DateOnly(2027, 6, 30), [existing]);

        Assert.Equal("SchoolYears.AlreadyExists", result.Error!.Code);
        Assert.Equal("2026-2027", Assert.Single(result.Error.Args));
    }

    [Fact]
    [Trait("spec", Spec + ": Un solo curso activo (Primer curso)")]
    public void The_first_year_of_the_system_is_created_active()
    {
        var result = AcademicYear.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30), []);

        Assert.True(result.Value!.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Un solo curso activo (Segundo curso mientras hay uno activo)")]
    public void A_later_year_is_created_inactive_and_the_active_one_does_not_change()
    {
        var active = Year(2026, active: true);

        var result = AcademicYear.Create(Guid.NewGuid(), new DateOnly(2027, 9, 1), new DateOnly(2028, 6, 30), [active]);

        Assert.False(result.Value!.IsActive);
        Assert.True(active.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Un solo curso activo (Segundo curso mientras hay uno activo)")]
    public void A_later_year_is_inactive_even_if_the_only_year_that_exists_is_inactive()
    {
        var old = Year(2025); // not active, e.g. closed

        var result = AcademicYear.Create(Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30), [old]);

        Assert.False(result.Value!.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Activación de un curso (Activar con otro activo)")]
    public void Activating_a_year_while_another_is_active_is_refused()
    {
        var active = Year(2026, active: true);
        var next = Year(2027);

        var result = next.Activate([active, next]);

        Assert.Equal("SchoolYears.AnotherActive", result.Error!.Code);
        Assert.False(next.IsActive);
        Assert.True(active.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Activación de un curso (Activar sin curso activo)")]
    public void Activating_a_year_when_none_is_active_makes_it_the_active_one()
    {
        var old = Year(2025);
        var next = Year(2026);

        var result = next.Activate([old, next]);

        Assert.True(result.IsSuccess);
        Assert.True(next.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Activación de un curso (Activar sin curso activo)")]
    public void Activating_the_year_that_is_already_active_changes_nothing()
    {
        var active = Year(2026, active: true);

        Assert.True(active.Activate([active]).IsSuccess);
        Assert.True(active.IsActive);
    }

    [Theory]
    [InlineData(YearOperation.AddEnrollment)]
    [InlineData(YearOperation.ChangeEnrollment)]
    [InlineData(YearOperation.OpenAssignment)]
    [InlineData(YearOperation.ChangeAssignment)]
    [InlineData(YearOperation.CloseAssignment)]
    [Trait("spec", Spec + ": Histórico de solo lectura (Modificación en un curso anterior)")]
    public void Only_the_active_year_accepts_changes_whatever_the_operation(YearOperation operation)
    {
        Assert.Null(YearGuard.Check(Year(2026, active: true), operation));
        Assert.Equal("SchoolYears.NotActive", YearGuard.Check(Year(2025), operation)!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Un solo curso activo (Sin curso activo)")]
    public void Without_a_year_the_guard_says_to_create_or_activate_one()
    {
        Assert.Equal("SchoolYears.NoActiveYear", YearGuard.Check(null, YearOperation.AddEnrollment)!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Eliminación de cursos (Curso vacío)")]
    public void A_year_without_data_can_be_deleted_and_one_with_data_cannot()
    {
        var year = Year(2026);

        Assert.True(year.CheckCanDelete(hasData: false).IsSuccess);
        Assert.Equal("SchoolYears.HasData", year.CheckCanDelete(hasData: true).Error!.Code);
    }
}
