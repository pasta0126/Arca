// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Common;

/// <summary>
/// One transaction per use case (docs/convenciones.md, section 4). Everything the work saves, and the events it records,
/// is committed together if the result is a success and undone if it is a failure or the work throws. Infrastructure
/// implements it over the database; the tests use an in-memory one.
/// </summary>
public interface IUnitOfWork
{
    Task<Result<T>> RunAsync<T>(Func<CancellationToken, Task<Result<T>>> work, CancellationToken ct);
}
