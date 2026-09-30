// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Common;
using Arca.Application.Home;
using Arca.Application.Localization;
using Arca.Application.Lockers.ListLockerRows;
using Arca.Application.Students.ListStudentRows;
using Arca.Application.Students.SearchStudents;
using Arca.Infrastructure.Inventory;
using Arca.UI.Home;

namespace Arca.Desktop.Composition;

/// <summary>
/// Joins the start screen to the use cases of its cards (the composition root is the only place of the desktop project that knows them): the
/// counts are read with the same queries the lockers and the students screens use, so a card and its screen always agree.
/// </summary>
static class HomeCardsComposition
{
    public static HomeCardServices Create(EfInventory store, IClock clock, ILocalizer localizer)
    {
        var occupancy = new AssignmentOccupancy(store.Assignments, store.Lockers);
        var lockerRows = new ListLockerRowsHandler(store.Lockers, store.Zones, store.Assignments, store.Students, store.Charges);
        var studentRows = new ListStudentRowsHandler(new SearchStudentsHandler(store.Students, store.Enrollments, store.Catalog, store.Years, occupancy), store.Charges);
        var defaults = new EnsureDefaultCardsHandler(store.HomeCards, store, clock, localizer);
        return new HomeCardServices(
            defaults.HandleAsync,
            defaults.RestoreAsync,
            new GetHomeCardsHandler(store.HomeCards, store.Zones, store.Catalog, store.Years, lockerRows.HandleAsync, studentRows.HandleAsync).HandleAsync,
            new ResolveHomeCardHandler(store.HomeCards, store.Zones, store.Catalog, lockerRows.HandleAsync).HandleAsync,
            new CreateHomeCardHandler(store.HomeCards, store).HandleAsync,
            new EditHomeCardHandler(store.HomeCards, store).HandleAsync,
            new MoveHomeCardHandler(store.HomeCards, store).HandleAsync,
            new DeleteHomeCardHandler(store.HomeCards, store).HandleAsync,
            new GetCardOptionsHandler(store.Zones, store.Catalog).HandleAsync,
            new PreviewHomeCardHandler(lockerRows.HandleAsync, studentRows.HandleAsync).HandleAsync);
    }
}
