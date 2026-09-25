using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arca.Infrastructure.Storage.Migrations
{
    /// <inheritdoc />
    public partial class TaquillesIZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Zones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    NameKey = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lockers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OutOfService = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsReserved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReservationNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RetiredAtUtc = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lockers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lockers_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LockerEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LockerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    BeforeJson = table.Column<string>(type: "TEXT", nullable: true),
                    AfterJson = table.Column<string>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockerEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LockerEvents_Lockers_LockerId",
                        column: x => x.LockerId,
                        principalTable: "Lockers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LockerEvents_LockerId_OccurredAtUtc",
                table: "LockerEvents",
                columns: new[] { "LockerId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Lockers_Number",
                table: "Lockers",
                column: "Number",
                unique: true,
                filter: "\"RetiredAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lockers_ZoneId",
                table: "Lockers",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Zones_NameKey",
                table: "Zones",
                column: "NameKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LockerEvents");

            migrationBuilder.DropTable(
                name: "Lockers");

            migrationBuilder.DropTable(
                name: "Zones");
        }
    }
}
