using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class AddSheetFromFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedCount",
                table: "FilesForExport");

            migrationBuilder.CreateTable(
                name: "SheetsFromFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetName = table.Column<string>(type: "text", nullable: false),
                    ProcessedCount = table.Column<long>(type: "bigint", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetsFromFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SheetsFromFiles_FilesForExport_Id",
                        column: x => x.Id,
                        principalTable: "FilesForExport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SheetsFromFiles");

            migrationBuilder.AddColumn<long>(
                name: "ProcessedCount",
                table: "FilesForExport",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
