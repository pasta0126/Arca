// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.SchoolYears;
using Arca.Application.SchoolYears.ActivateAcademicYear;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.SchoolYears.DeleteAcademicYear;
using Arca.Application.SchoolYears.GetAcademicYear;
using Arca.Application.SchoolYears.ListAcademicYears;
using Arca.Domain.SchoolYears;
using Arca.Testing.Inventory;
using Xunit;

namespace Arca.Application.Tests.Inventory;

public sealed class SchoolYearUseCaseTests
{
    const string Spec = "alumnes-i-assignacions/curs-escolar";

    static CreateAcademicYearHandler Create(InMemoryInventory store) => new(store.Years, store);

    static Task<Arca.Domain.Common.Result<AcademicYearSummary>> CreateYear(InMemoryInventory store, int startYear) =>
        Create(store).HandleAsync(new CreateAcademicYearRequest(new DateOnly(startYear, 9, 1), new DateOnly(startYear + 1, 6, 30)), default);

    [Fact]
    [Trait("spec", Spec + ": Crear un curso escolar (Curso nuevo)")]
    public async Task The_first_year_is_saved_active_with_its_derived_name()
    {
        var store = new InMemoryInventory();

        var result = await CreateYear(store, 2026);

        Assert.Equal("2026-2027", result.Value!.Name);
        Assert.True(result.Value.IsActive);
        Assert.Single(store.YearList);
    }

    [Fact]
    [Trait("spec", Spec + ": Un solo curso activo (Segundo curso mientras hay uno activo)")]
    public async Task A_second_year_is_saved_without_activating_it()
    {
        var store = new InMemoryInventory();
        await CreateYear(store, 2026);

        var second = await CreateYear(store, 2027);

        Assert.False(second.Value!.IsActive);
        Assert.Single(store.YearList, y => y.IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Crear un curso escolar (Solapamiento con otro curso)")]
    public async Task Invalid_overlapping_and_duplicate_years_save_nothing()
    {
        var store = new InMemoryInventory();
        await CreateYear(store, 2026);
        var handler = Create(store);

        var dates = await handler.HandleAsync(new CreateAcademicYearRequest(new DateOnly(2028, 9, 1), new DateOnly(2028, 9, 1)), default);
        var overlap = await handler.HandleAsync(new CreateAcademicYearRequest(new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 1)), default);
        var duplicate = await handler.HandleAsync(new CreateAcademicYearRequest(new DateOnly(2026, 10, 1), new DateOnly(2027, 6, 1)), default);

        Assert.Equal("SchoolYears.DatesInvalid", dates.Error!.Code);
        Assert.Equal("SchoolYears.Overlaps", overlap.Error!.Code);
        Assert.Equal("SchoolYears.AlreadyExists", duplicate.Error!.Code);
        Assert.Single(store.YearList);
    }

    [Fact]
    [Trait("spec", Spec + ": Activación de un curso (Activar con otro activo)")]
    public async Task Activating_needs_no_other_active_year()
    {
        var store = new InMemoryInventory();
        await CreateYear(store, 2026);
        var second = (await CreateYear(store, 2027)).Value!;
        var activate = new ActivateAcademicYearHandler(store.Years, store);

        var refused = await activate.HandleAsync(new ActivateAcademicYearRequest(second.Id), default);

        Assert.Equal("SchoolYears.AnotherActive", refused.Error!.Code);
        Assert.False(store.YearList.Single(y => y.Id == second.Id).IsActive);
    }

    [Fact]
    [Trait("spec", Spec + ": Activación de un curso (Activar sin curso activo)")]
    public async Task With_no_active_year_a_year_can_be_activated_and_an_unknown_one_is_reported()
    {
        var store = new InMemoryInventory();
        await CreateYear(store, 2026);
        var second = (await CreateYear(store, 2027)).Value!;
        var first = store.YearList.Single(y => y.IsActive); // deactivate it the way the closing of cursos-i-historial will
        store.YearList[store.YearList.IndexOf(first)] = AcademicYear.Restore(first.Id, first.StartDate, first.EndDate, isActive: false);
        var activate = new ActivateAcademicYearHandler(store.Years, store);

        var done = await activate.HandleAsync(new ActivateAcademicYearRequest(second.Id), default);
        var missing = await activate.HandleAsync(new ActivateAcademicYearRequest(Guid.NewGuid()), default);

        Assert.True(done.Value!.IsActive);
        Assert.Equal("SchoolYears.NotFound", missing.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Histórico de solo lectura (Consulta de un curso anterior)")]
    public async Task Every_year_can_be_listed_and_read_most_recent_first()
    {
        var store = new InMemoryInventory();
        await CreateYear(store, 2025);
        var third = (await CreateYear(store, 2027)).Value!;
        await CreateYear(store, 2026);

        var list = await new ListAcademicYearsHandler(store.Years).HandleAsync(default);
        var detail = await new GetAcademicYearHandler(store.Years).HandleAsync(new GetAcademicYearRequest(third.Id), default);
        var missing = await new GetAcademicYearHandler(store.Years).HandleAsync(new GetAcademicYearRequest(Guid.NewGuid()), default);

        Assert.Equal(["2027-2028", "2026-2027", "2025-2026"], list.Value!.Select(y => y.Name));
        Assert.Equal("2027-2028", detail.Value!.Name);
        Assert.Equal("SchoolYears.NotFound", missing.Error!.Code);
    }

    [Fact]
    [Trait("spec", Spec + ": Eliminación de cursos (Curso vacío)")]
    public async Task A_year_with_no_data_is_deleted_and_one_with_data_is_kept()
    {
        var store = new InMemoryInventory();
        var empty = (await CreateYear(store, 2026)).Value!;
        var used = (await CreateYear(store, 2027)).Value!;
        store.YearsWithData.Add(used.Id);
        var delete = new DeleteAcademicYearHandler(store.Years, store);

        var deleted = await delete.HandleAsync(new DeleteAcademicYearRequest(empty.Id), default);
        var refused = await delete.HandleAsync(new DeleteAcademicYearRequest(used.Id), default);
        var missing = await delete.HandleAsync(new DeleteAcademicYearRequest(Guid.NewGuid()), default);

        Assert.True(deleted.IsSuccess);
        Assert.Equal("SchoolYears.HasData", refused.Error!.Code);
        Assert.Equal("SchoolYears.NotFound", missing.Error!.Code);
        Assert.Equal([used.Id], store.YearList.Select(y => y.Id));
    }

    [Fact]
    [Trait("spec", Spec + ": Un solo curso activo (Sin curso activo)")]
    public async Task Writing_enrolments_needs_an_active_year_and_says_to_create_or_activate_one()
    {
        var store = new InMemoryInventory();

        var none = await ActiveYear.RequireAsync(store.Years, YearOperation.AddEnrollment, default);
        await CreateYear(store, 2026);
        var some = await ActiveYear.RequireAsync(store.Years, YearOperation.AddEnrollment, default);

        Assert.Equal("SchoolYears.NoActiveYear", none.Error!.Code);
        Assert.Equal("2026-2027", some.Value!.Name);
    }
}
