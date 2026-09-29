// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.ConceptAmounts.GetConceptAmounts;
using Arca.Application.ConceptAmounts.GetConceptAmountsHistory;
using Arca.Application.ConceptAmounts.SetConceptAmounts;
using Arca.Application.Localization;
using Arca.Application.SchoolYears.ActivateAcademicYear;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.SchoolYears.DeleteAcademicYear;
using Arca.Application.SchoolYears.GetYearScreen;
using Arca.Application.SchoolYears.ListAcademicYears;
using Arca.Infrastructure.Inventory;
using Arca.UI.Course;

namespace Arca.Desktop.Composition;

/// <summary>Joins the Course section to its use cases: the composition root is the only place of the desktop project that knows them.</summary>
static class CourseComposition
{
    public static CourseServices Create(EfInventory store, IClock clock, ILocalizer localizer) => new(
        new ListAcademicYearsHandler(store.Years).HandleAsync,
        (id, ct) => new GetYearScreenHandler(store.Years, store.ConceptAmounts, store.Charges, clock).HandleAsync(new GetYearScreenRequest(id), ct),
        new CreateAcademicYearHandler(store.Years, store).HandleAsync,
        (id, ct) => new ActivateAcademicYearHandler(store.Years, store).HandleAsync(new ActivateAcademicYearRequest(id), ct),
        (id, ct) => new DeleteAcademicYearHandler(store.Years, store).HandleAsync(new DeleteAcademicYearRequest(id), ct),
        (id, ct) => new GetConceptAmountsHandler(store.Years, store.ConceptAmounts, clock).HandleAsync(new GetConceptAmountsRequest(id), ct),
        new SetConceptAmountsHandler(store.Years, store.ConceptAmounts, store.ConceptAmountEvents, store, clock).HandleAsync,
        (id, ct) => new GetConceptAmountsHistoryHandler(store.Years, store.ConceptAmounts, store.ConceptAmountEvents, localizer)
            .HandleAsync(new GetConceptAmountsHistoryRequest(id), ct));
}
