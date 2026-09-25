// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Storage;

/// <summary>
/// Supplies the key that opens the database at a path. The real implementation asks for the centre password
/// (acces-i-xifrat); a development-only provider reads it from the environment.
/// </summary>
public interface IDatabaseKeyProvider
{
    Task<Result<DatabaseKey>> GetKeyAsync(string databasePath, CancellationToken ct = default);
}
