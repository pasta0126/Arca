// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo

using Arca.Application.Storage;
using Arca.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Arca.Infrastructure.Tests.Storage;

// Each context owns its own hand-written migrations (EF matches them by the DbContext attribute),
// so a test can model "older file", "newer file" and "broken migration" without touching the real schema.

sealed class V1Context(string path, DatabaseKey key, bool create = false) : ArcaDbContext(path, key, create);

sealed class V2Context(string path, DatabaseKey key, bool create = false) : ArcaDbContext(path, key, create);

sealed class BrokenContext(string path, DatabaseKey key, bool create = false) : ArcaDbContext(path, key, create);

[DbContext(typeof(V1Context))]
[Migration("001_CreateA")]
sealed class V1CreateA : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("CREATE TABLE A (Id INTEGER PRIMARY KEY)");
}

[DbContext(typeof(V2Context))]
[Migration("001_CreateA")]
sealed class V2CreateA : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("CREATE TABLE A (Id INTEGER PRIMARY KEY)");
}

[DbContext(typeof(V2Context))]
[Migration("002_CreateB")]
sealed class V2CreateB : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("CREATE TABLE B (Id INTEGER PRIMARY KEY)");
}

[DbContext(typeof(BrokenContext))]
[Migration("001_CreateA")]
sealed class BrokenCreateA : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("CREATE TABLE A (Id INTEGER PRIMARY KEY)");
}

[DbContext(typeof(BrokenContext))]
[Migration("002_Broken")]
sealed class BrokenHalfway : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TABLE C (Id INTEGER PRIMARY KEY)");
        migrationBuilder.Sql("THIS IS NOT SQL");
    }
}

/// <summary>A clock that moves one second on every read, so copy names are distinct and ordered.</summary>
sealed class SteppingTime(DateTimeOffset start) : TimeProvider
{
    DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now = _now.AddSeconds(1);
}
