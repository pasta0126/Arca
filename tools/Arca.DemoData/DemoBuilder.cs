// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Assignments.AssignLocker;
using Arca.Application.Charges;
using Arca.Application.Charges.MarkChargeExempt;
using Arca.Application.Charges.MarkChargePaid;
using Arca.Application.Charges.WaiveCharge;
using Arca.Application.ConceptAmounts.SetConceptAmounts;
using Arca.Application.Lockers.CreateLockerRange;
using Arca.Application.Lockers.MarkLockerOutOfService;
using Arca.Application.Lockers.ReserveLocker;
using Arca.Application.SchoolYears.CreateAcademicYear;
using Arca.Application.Students;
using Arca.Application.Students.AddStudent;
using Arca.Application.Students.RetireStudent;
using Arca.Application.Zones.CreateZone;
using Arca.Domain.Charges;
using Arca.Domain.Common;
using Arca.Domain.ConceptAmounts;
using Arca.Domain.Lockers;
using Arca.Infrastructure.Common;
using Arca.Infrastructure.Inventory;
using Arca.Infrastructure.Storage;

namespace Arca.DemoData;

/// <summary>
/// Fills the database of a demonstration centre by calling the same use cases the application will call, so every rule, index
/// and history is respected (docs/datos-de-ejemplo.md, profile demo): 6 zones with 600 lockers, 900 students in 20 groups, about
/// 520 lockers in use, a mix of paid, exempt and pending charges, some debt of the previous year, some students who left with
/// their deposit to return, and lockers reserved, broken and under maintenance. The same seed gives the same centre.
/// </summary>
/// <summary>What the generated centre is like: <see cref="Demo"/> is a centre in the middle of the year, <see cref="NewYear"/> one that has just started it.</summary>
enum DemoProfile
{
    /// <summary>Students with lockers, payments, debts and leavers, as in the middle of the school year.</summary>
    Demo,

    /// <summary>The first day of a new school year: the year active with its amounts, the zones and lockers set up, the students enrolled and nobody assigned yet; some carry debt of the year before.</summary>
    NewYear,
}

sealed class DemoBuilder(Func<ArcaDbContext> createContext, Random random, DemoProfile profile = DemoProfile.Demo)
{
    const int Namesakes = 6;

    const int StudentCount = 900;
    const int InitiallyAssigned = 540;
    const int DebtorsOfTheYearBefore = 60;
    const int Retired = 20;
    const int Reserved = 5;
    const int Broken = 15;
    const int Maintenance = 10;
    const int WithPreviousDebt = 40;

    public async Task BuildAsync(Action<string> say)
    {
        var store = new EfInventory(createContext);
        var clock = new SystemClock();
        var occupancy = new AssignmentOccupancy(store.Assignments, store.Lockers);
        var services = new AssignmentServices(
            store.Assignments, store.Students, store.Lockers, store.Zones, store.Enrollments, store.Years, store.StudentEvents, store.Events,
            [new ChargeGenerationGuard(store.ConceptAmounts, store.Charges)],
            [new ChargeGenerationHandler(store.ConceptAmounts, store.Charges, store.ChargeEvents, clock)], []);

        // Years and amounts: the active year, and the previous one, already finished.
        say("Cursos i imports…");
        var year = (await new CreateAcademicYearHandler(store.Years, store).HandleAsync(new CreateAcademicYearRequest(new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30)), default)).Value!;
        var previous = (await new CreateAcademicYearHandler(store.Years, store).HandleAsync(new CreateAcademicYearRequest(new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 30)), default)).Value!;
        await new SetConceptAmountsHandler(store.Years, store.ConceptAmounts, store.ConceptAmountEvents, store, clock)
            .HandleAsync(new SetConceptAmountsRequest(year.Id, 50m, 20m, 10m), default);

        // Zones and lockers.
        say("Zones i taquilles…");
        var zoneHandler = new CreateZoneHandler(store.Zones, store);
        var oldStore = (await zoneHandler.HandleAsync(new CreateZoneRequest("Antic magatzem"), default)).Value!; // a zone that is no longer used, to try the filters
        await new Arca.Application.Zones.DeactivateZone.DeactivateZoneHandler(store.Zones, store.Lockers, store)
            .HandleAsync(new Arca.Application.Zones.DeactivateZone.DeactivateZoneRequest(oldStore.Id), default);
        var rangeHandler = new CreateLockerRangeHandler(store.Lockers, store.Zones, store.Events, store, clock);
        var lockerIds = new List<Guid>();
        var next = 1;
        for (var z = 0; z < Names.Zones.Length; z++)
        {
            var zone = (await zoneHandler.HandleAsync(new CreateZoneRequest(Names.Zones[z]), default)).Value!;
            var request = new CreateLockerRangeRequest(next, next + Names.ZoneSizes[z] - 1, zone.Id);
            var plan = (await rangeHandler.AnalyzeAsync(request, null, default)).Value!;
            await rangeHandler.ApplyAsync(plan, null, default);
            next += Names.ZoneSizes[z];
        }

        var lockers = (await store.Lockers.ListAsync(includeRetired: false, default)).OrderBy(l => l.Number).ToList();
        lockerIds.AddRange(lockers.Select(l => l.Id));
        var order = lockerIds.OrderBy(_ => random.Next()).ToList();

        // Lockers that will not be assigned: reserved, broken and under maintenance.
        say("Taquilles reservades, avariades i en manteniment…");
        var reserve = new ReserveLockerHandler(store.Lockers, store.Zones, store.Events, occupancy, store, clock);
        var outOfService = new MarkLockerOutOfServiceHandler(store.Lockers, store.Zones, store.Events, occupancy, services, store, clock);
        var pool = new Queue<Guid>(order);
        for (var i = 0; i < Reserved; i++)
        {
            await reserve.HandleAsync(new ReserveLockerRequest(pool.Dequeue(), "Reservada per al professorat"), default);
        }

        for (var i = 0; i < Broken; i++)
        {
            await outOfService.HandleAsync(new MarkLockerOutOfServiceRequest(pool.Dequeue(), OutOfServiceKind.Broken), default);
        }

        for (var i = 0; i < Maintenance; i++)
        {
            await outOfService.HandleAsync(new MarkLockerOutOfServiceRequest(pool.Dequeue(), OutOfServiceKind.Maintenance), default);
        }

        // Students, spread over the groups.
        say($"{StudentCount} alumnes…");
        var add = new AddStudentHandler(store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, store, clock);
        var students = new List<Guid>();
        var used = new HashSet<string>();
        for (var i = 0; i < StudentCount; i++)
        {
            var (first, last) = (Names.First[random.Next(Names.First.Length)], Names.Last[random.Next(Names.Last.Length)] + " " + Names.Last[random.Next(Names.Last.Length)]);
            var email = $"{Slug(first)}.{Slug(last)}{i}@test.cat";
            if (!used.Add(email))
            {
                continue;
            }

            var level = Names.Levels[i % Names.Levels.Length];
            var group = Names.Groups[i / Names.Levels.Length % (level.Contains("Batxillerat", StringComparison.Ordinal) ? 2 : 4)];
            var added = await add.HandleAsync(new AddStudentRequest(first, last, email, level, group, ConfirmNewValues: true), default);
            students.Add(added.Value!.Student!.Id);
        }

        // Namesakes: the same name and surnames on different people, which a real centre has and the forms warn about.
        for (var i = 0; i < Namesakes; i++)
        {
            var first = Names.First[i];
            var last = Names.Last[i] + " " + Names.Last[i + 1];
            for (var copy = 0; copy < 2; copy++)
            {
                var level = Names.Levels[(i + copy * 2) % Names.Levels.Length];
                var added = await add.HandleAsync(new AddStudentRequest(first, last, $"{Slug(first)}.{Slug(last)}.h{copy}@test.cat", level, "A", ConfirmNewValues: true), default);
                students.Add(added.Value!.Student!.Id);
            }
        }

        if (profile == DemoProfile.NewYear)
        {
            await FinishNewYearAsync(store, clock, students, previous.Id, say);
            return;
        }

        // Assignments, which generate the fee and the deposit of each student.
        say($"{InitiallyAssigned} assignacions…");
        var assign = new AssignLockerHandler(services, store, clock);
        var assigned = new List<Guid>();
        foreach (var student in students.Take(InitiallyAssigned))
        {
            var result = await assign.HandleAsync(new AssignLockerRequest(student, pool.Dequeue(), ConfirmWarnings: true), default);
            if (result.IsSuccess)
            {
                assigned.Add(student);
            }
        }

        // Payments: most have paid, a few are exempt or waived, the rest owe.
        say("Cobraments…");
        var paid = new MarkChargePaidHandler(store.Charges, store.ChargeEvents, store.Students, store.Years, store, clock);
        var exempt = new MarkChargeExemptHandler(store.Charges, store.ChargeEvents, store.Students, store.Years, store, clock);
        var waive = new WaiveChargeHandler(store.Charges, store.ChargeEvents, store.Students, store.Years, store, clock);
        foreach (var student in assigned)
        {
            foreach (var charge in await store.Charges.ListByStudentAsync(student, default))
            {
                var roll = random.Next(100);
                if (roll < 62)
                {
                    await paid.HandleAsync(new MarkChargePaidRequest(charge.Id, null), default);
                }
                else if (roll < 66)
                {
                    await exempt.HandleAsync(new MarkChargeExemptRequest(charge.Id, "Beca de menjador"), default);
                }
                else if (roll < 68)
                {
                    await waive.HandleAsync(new WaiveChargeRequest(charge.Id, "Situació familiar"), default);
                }
            }
        }

        // Debt of the previous year for some of them.
        say("Deute del curs anterior…");
        await store.RunAsync(async ct =>
        {
            foreach (var student in assigned.OrderBy(_ => random.Next()).Take(WithPreviousDebt))
            {
                var old = Charge.Create(Guid.NewGuid(), student, ChargeConcept.Fee, previous.Id, Money.FromCents(4500), clock.UtcNow);
                await store.Charges.AddAsync(old.Charge, ct);
                await store.ChargeEvents.AddAsync(old.Event, ct);
            }

            return Result<bool>.Success(true);
        }, default);

        // Students who left, with their deposit to be returned.
        say("Baixes…");
        var retire = new RetireStudentHandler(
            store.Students, store.Enrollments, store.Catalog, store.Years, store.StudentEvents, occupancy, services,
            [new DepositLifecycleHandler(store.Charges, store.ChargeEvents, clock)], store, clock);
        foreach (var student in assigned.OrderBy(_ => random.Next()).Take(Retired))
        {
            await retire.HandleAsync(new RetireStudentRequest(student, "Trasllat a un altre centre"), default);
        }

        var map = (await new Arca.Application.LockerMap.GetLockerMapHandler(store.Zones, store.Lockers, store.Assignments, store.Students, store.Charges).HandleAsync(default)).Value!;
        var state = (await new Arca.Application.GlobalState.GetGlobalStateHandler(store.Years, store.Charges).HandleAsync(default)).Value!;
        var withDebt = map.Zones.SelectMany(z => z.Lockers).Count(l => l.HasDebt);
        say($"Fet: {map.Counters.Active} taquilles ({map.Counters.Free} lliures, {map.Counters.Occupied} ocupades, {map.Counters.Reserved} reservades, {map.Counters.Broken} avariades, {map.Counters.Maintenance} en manteniment), " +
            $"{withDebt} amb deute, {state.PendingCharges} càrrecs pendents, curs {state.ActiveYear!.Name}.");
    }

    /// <summary>The new year at its start: nobody has a locker yet, but some students still owe the year before, so the warning shows when they are assigned.</summary>
    async Task FinishNewYearAsync(EfInventory store, SystemClock clock, List<Guid> students, Guid previousYear, Action<string> say)
    {
        say("Deute del curs anterior…");
        await store.RunAsync(async ct =>
        {
            foreach (var student in students.OrderBy(_ => random.Next()).Take(DebtorsOfTheYearBefore))
            {
                var old = Charge.Create(Guid.NewGuid(), student, ChargeConcept.Fee, previousYear, Money.FromCents(4500), clock.UtcNow);
                await store.Charges.AddAsync(old.Charge, ct);
                await store.ChargeEvents.AddAsync(old.Event, ct);
            }

            return Result<bool>.Success(true);
        }, default);
        var map = (await new Arca.Application.LockerMap.GetLockerMapHandler(store.Zones, store.Lockers, store.Assignments, store.Students, store.Charges).HandleAsync(default)).Value!;
        var state = (await new Arca.Application.GlobalState.GetGlobalStateHandler(store.Years, store.Charges).HandleAsync(default)).Value!;
        say($"Fet: curs {state.ActiveYear!.Name} nou, {map.Counters.Active} taquilles ({map.Counters.Free} lliures, {map.Counters.Reserved} reservades, {map.Counters.Broken} avariades, " +
            $"{map.Counters.Maintenance} en manteniment), {students.Count} alumnes matriculats sense taquilla, {state.PendingCharges} càrrecs pendents del curs anterior.");
    }

    static string Slug(string text) =>
        new(text.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => char.IsLetter(c) && c < 128)
            .Select(char.ToLowerInvariant)
            .ToArray());
}
