using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedRunsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<string>(type: "text", nullable: false),
                    ProspectsCount = table.Column<long>(type: "bigint", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedRuns_RunId",
                table: "ProcessedRuns",
                column: "RunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedRuns");
        }
    }
}
