using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arca.Infrastructure.Storage.Migrations
{
    /// <inheritdoc />
    public partial class HomeCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Target = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CriteriaText = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    SeedKey = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HomeCardsState",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultsCreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeCardsState", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeCards_Position",
                table: "HomeCards",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_HomeCards_SeedKey",
                table: "HomeCards",
                column: "SeedKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeCards");

            migrationBuilder.DropTable(
                name: "HomeCardsState");
        }
    }
}
