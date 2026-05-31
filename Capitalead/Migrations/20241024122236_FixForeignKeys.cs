using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class FixForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExportedSpreadsheets_FilesForExport_Id",
                table: "ExportedSpreadsheets");

            migrationBuilder.DropForeignKey(
                name: "FK_SheetsFromFiles_FilesForExport_Id",
                table: "SheetsFromFiles");

            migrationBuilder.CreateIndex(
                name: "IX_SheetsFromFiles_FileId",
                table: "SheetsFromFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportedSpreadsheets_FileId",
                table: "ExportedSpreadsheets",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExportedSpreadsheets_FilesForExport_FileId",
                table: "ExportedSpreadsheets",
                column: "FileId",
                principalTable: "FilesForExport",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SheetsFromFiles_FilesForExport_FileId",
                table: "SheetsFromFiles",
                column: "FileId",
                principalTable: "FilesForExport",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExportedSpreadsheets_FilesForExport_FileId",
                table: "ExportedSpreadsheets");

            migrationBuilder.DropForeignKey(
                name: "FK_SheetsFromFiles_FilesForExport_FileId",
                table: "SheetsFromFiles");

            migrationBuilder.DropIndex(
                name: "IX_SheetsFromFiles_FileId",
                table: "SheetsFromFiles");

            migrationBuilder.DropIndex(
                name: "IX_ExportedSpreadsheets_FileId",
                table: "ExportedSpreadsheets");

            migrationBuilder.AddForeignKey(
                name: "FK_ExportedSpreadsheets_FilesForExport_Id",
                table: "ExportedSpreadsheets",
                column: "Id",
                principalTable: "FilesForExport",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SheetsFromFiles_FilesForExport_Id",
                table: "SheetsFromFiles",
                column: "Id",
                principalTable: "FilesForExport",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
