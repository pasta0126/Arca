// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Domain.Common;

namespace Arca.Application.Storage;

/// <summary>
/// Supplies the key that opens the database. The real implementation is the access flow of acces-i-xifrat
/// (centre password or recovery key). Until it exists, a development-only provider is used.
/// </summary>
public interface IDatabaseKeyProvider
{
    Task<Result<DatabaseKey>> GetKeyAsync(CancellationToken ct = default);
}
