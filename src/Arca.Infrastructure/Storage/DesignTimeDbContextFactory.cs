// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Microsoft.EntityFrameworkCore.Design;

namespace Arca.Infrastructure.Storage;

/// <summary>
/// Used only by the 'dotnet ef migrations' tooling to read the model. It never opens a real database
/// (adding a migration does not connect), so the throwaway key protects nothing.
/// </summary>
sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ArcaDbContext>
{
    public ArcaDbContext CreateDbContext(string[] args) =>
        new(Path.Combine(Path.GetTempPath(), "arca-design-time.db"), new DatabaseKey(new byte[DatabaseKey.Length]));
}
