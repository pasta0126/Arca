// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Common;
using Arca.Application.Lockers;
using Arca.Application.Zones;
using Arca.Domain.Common;
using Arca.Domain.Lockers;
using Arca.Domain.Zones;
using Arca.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Arca.Infrastructure.Inventory;

/// <summary>
/// The zones, lockers and their history over the encrypted database. As a unit of work it opens one database context and
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
    }

    public IZoneRepository Zones { get; }

    public ILockerRepository Lockers { get; }

    public ILockerEventRepository Events { get; }

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
