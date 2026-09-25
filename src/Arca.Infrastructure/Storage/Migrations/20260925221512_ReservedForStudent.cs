using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arca.Infrastructure.Storage.Migrations
{
    /// <inheritdoc />
    public partial class ReservedForStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReservedForStudentId",
                table: "Lockers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservedForStudentId",
                table: "Lockers");
        }
    }
}
