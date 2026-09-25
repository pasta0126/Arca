// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Domain.Common;

namespace Arca.Application.Security;

/// <summary>
/// First run, when there is no database yet (hito 1: password only, in the default folder): asks for the password,
/// shows and confirms the recovery key and creates the database with its key file. It returns the new database key,
/// or <see cref="KeyErrors.UnlockCancelled"/> if the person gives up, in which case nothing has been created.
/// </summary>
public interface IFirstRunFlow
{
    Task<Result<DatabaseKey>> CreateAsync(string databasePath, CancellationToken ct);
}
