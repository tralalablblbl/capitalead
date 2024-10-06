using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class FilesForExportTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilesForExport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    Exported = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilesForExport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportedSpreadsheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportedSpreadsheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportedSpreadsheets_FilesForExport_Id",
                        column: x => x.Id,
                        principalTable: "FilesForExport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExportedSpreadsheets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportedSpreadsheets_UserId",
                table: "ExportedSpreadsheets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FilesForExport_FileName",
                table: "FilesForExport",
                column: "FileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportedSpreadsheets");

            migrationBuilder.DropTable(
                name: "FilesForExport");
        }
    }
}
