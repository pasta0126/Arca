// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Assignments;
using Arca.Application.Catalog;
using Arca.Application.Charges;
using Arca.Application.ConceptAmounts;
using Arca.Application.Common;
using Arca.Application.Enrollments;
using Arca.Application.Lockers;
using Arca.Application.SchoolYears;
using Arca.Application.Students;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.Zones;
using Arca.Infrastructure.Assignments;
using Arca.Infrastructure.Catalog;
using Arca.Infrastructure.Charges;
using Arca.Infrastructure.ConceptAmounts;
using Arca.Infrastructure.Enrollments;
using Arca.Infrastructure.SchoolYears;
using Arca.Infrastructure.Storage;
using Arca.Infrastructure.Students;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Inventory;

/// <summary>
/// Every repository over the encrypted database: zones and lockers (taquilles-i-zones), and school years, students,
/// enrolments, catalogue and assignments (alumnes-i-assignacions). As a unit of work it opens one database context and
/// one transaction for a use case, and the repositories use that context while it runs (docs/convenciones.md, section 4):
/// what the work saves is committed together if it succeeds and undone if it fails or throws. A read that runs outside a
/// unit of work uses a short-lived context of its own. Nothing is loaded lazily.
/// </summary>
/// <param name="createContext">Makes a context over the open database, for example <c>StorageSession.CreateContext</c>.</param>
public sealed class EfInventory : IUnitOfWork
{
    readonly Func<ArcaDbContext> _createContext;
    readonly AsyncLocal<ArcaDbContext?> _current = new();

    public EfInventory(Func<ArcaDbContext> createContext)
    {
        _createContext = createContext;
        Zones = new EfZoneRepository(this);
        Lockers = new EfLockerRepository(this);
        Events = new EfLockerEventRepository(this);
        Years = new EfAcademicYearRepository(this);
        Students = new EfStudentRepository(this);
        Enrollments = new EfEnrollmentRepository(this);
        Catalog = new EfCatalogRepository(this);
        StudentEvents = new EfStudentEventRepository(this);
        Assignments = new EfAssignmentRepository(this);
        ConceptAmounts = new EfConceptAmountRepository(this);
        ConceptAmountEvents = new EfConceptAmountEventRepository(this);
        Charges = new EfChargeRepository(this);
        ChargeEvents = new EfChargeEventRepository(this);
        Identity = new Arca.Infrastructure.Identity.EfCentreIdentityRepository(this);
        HomeCards = new Arca.Infrastructure.Home.EfHomeCardRepository(this);
    }

    public Arca.Application.Identity.ICentreIdentityRepository Identity { get; }

    public Arca.Application.Home.IHomeCardRepository HomeCards { get; }

    public IZoneRepository Zones { get; }

    public ILockerRepository Lockers { get; }

    public ILockerEventRepository Events { get; }

    public IAcademicYearRepository Years { get; }

    public IStudentRepository Students { get; }

    public IEnrollmentRepository Enrollments { get; }

    public ICatalogRepository Catalog { get; }

    public IStudentEventRepository StudentEvents { get; }

    public IAssignmentRepository Assignments { get; }

    public IConceptAmountRepository ConceptAmounts { get; }

    public IConceptAmountEventRepository ConceptAmountEvents { get; }

    public IChargeRepository Charges { get; }

    public IChargeEventRepository ChargeEvents { get; }

    public async Task<Result<T>> RunAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken ct)
    {
        if (_current.Value is not null)
        {
            return await work(ct); // already inside a unit of work: join it
        }

        await using var context = _createContext();
        _current.Value = context;
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            var result = await work(ct);
            if (result.IsSuccess)
            {
                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            else
            {
                await transaction.RollbackAsync(ct);
            }

            return result;
        }
        finally
        {
            _current.Value = null;
        }
    }

    /// <summary>Runs a read or a write with the context of the running unit of work, or with a temporary one if there is none.</summary>
    internal async Task<T> UseAsync<T>(Func<ArcaDbContext, Task<T>> action)
    {
        if (_current.Value is { } context)
        {
            return await action(context);
        }

        await using var temporary = _createContext();
        return await action(temporary);
    }

    /// <summary>Runs a write. Writes only happen inside a unit of work, so they are part of its transaction.</summary>
    internal async Task WriteAsync(Func<ArcaDbContext, Task> action)
    {
        if (_current.Value is not { } context)
        {
            throw new InvalidOperationException("A write must run inside a unit of work.");
        }

        await action(context);
    }
}
