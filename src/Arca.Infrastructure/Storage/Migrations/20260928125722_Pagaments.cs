using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arca.Infrastructure.Storage.Migrations
{
    /// <inheritdoc />
    public partial class Pagaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Charges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Concept = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    YearId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReturnStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ReturnedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReturnNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Charges_AcademicYears_YearId",
                        column: x => x.YearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Charges_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConceptAmounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    YearId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Concept = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptAmounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConceptAmounts_AcademicYears_YearId",
                        column: x => x.YearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChargeEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChargeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    BeforeJson = table.Column<string>(type: "TEXT", nullable: true),
                    AfterJson = table.Column<string>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChargeEvents_Charges_ChargeId",
                        column: x => x.ChargeId,
                        principalTable: "Charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConceptAmountEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConceptAmountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    BeforeJson = table.Column<string>(type: "TEXT", nullable: true),
                    AfterJson = table.Column<string>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptAmountEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConceptAmountEvents_ConceptAmounts_ConceptAmountId",
                        column: x => x.ConceptAmountId,
                        principalTable: "ConceptAmounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeEvents_ChargeId_OccurredAtUtc",
                table: "ChargeEvents",
                columns: new[] { "ChargeId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Charges_StudentId",
                table: "Charges",
                column: "StudentId",
                unique: true,
                filter: "\"Concept\" = 'Deposit' AND \"Status\" <> 'Voided' AND \"ReturnStatus\" <> 'Returned'");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_StudentId_Status",
                table: "Charges",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Charges_StudentId_YearId",
                table: "Charges",
                columns: new[] { "StudentId", "YearId" },
                unique: true,
                filter: "\"Concept\" = 'Fee'");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_YearId_Status",
                table: "Charges",
                columns: new[] { "YearId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ConceptAmountEvents_ConceptAmountId_OccurredAtUtc",
                table: "ConceptAmountEvents",
                columns: new[] { "ConceptAmountId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConceptAmounts_YearId_Concept",
                table: "ConceptAmounts",
                columns: new[] { "YearId", "Concept" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargeEvents");

            migrationBuilder.DropTable(
                name: "ConceptAmountEvents");

            migrationBuilder.DropTable(
                name: "Charges");

            migrationBuilder.DropTable(
                name: "ConceptAmounts");
        }
    }
}
