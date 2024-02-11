using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class AddSpreadsheetsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Imports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Started = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Completed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AddedCount = table.Column<long>(type: "bigint", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imports", x => x.Id);
                });
            migrationBuilder.Sql(@"insert into ""Imports"" (""Id"", ""Started"", ""Status"", ""Completed"", ""AddedCount"")
            values ('c2d29867-3d0b-d497-9191-18a9d8ee7830'::uuid, '2024-02-04 10:00:00'::timestamp, 2, '2024-02-04 11:00:00'::timestamp, (select count(*) from ""Prospects""))");

            migrationBuilder.AddColumn<Guid>(
                name: "ImportId",
                table: "Prospects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("c2d29867-3d0b-d497-9191-18a9d8ee7830"));

            migrationBuilder.AddColumn<long>(
                name: "SpreadsheetId",
                table: "Prospects",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Spreadsheets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ClusterId = table.Column<string>(type: "text", nullable: false),
                    ClusterName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spreadsheets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prospects_ImportId",
                table: "Prospects",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_Prospects_SpreadsheetId",
                table: "Prospects",
                column: "SpreadsheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Spreadsheets_Title",
                table: "Spreadsheets",
                column: "Title",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Prospects_Imports_ImportId",
                table: "Prospects",
                column: "ImportId",
                principalTable: "Imports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Prospects_Spreadsheets_SpreadsheetId",
                table: "Prospects",
                column: "SpreadsheetId",
                principalTable: "Spreadsheets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prospects_Imports_ImportId",
                table: "Prospects");

            migrationBuilder.DropForeignKey(
                name: "FK_Prospects_Spreadsheets_SpreadsheetId",
                table: "Prospects");

            migrationBuilder.DropTable(
                name: "Imports");

            migrationBuilder.DropTable(
                name: "Spreadsheets");

            migrationBuilder.DropIndex(
                name: "IX_Prospects_ImportId",
                table: "Prospects");

            migrationBuilder.DropIndex(
                name: "IX_Prospects_SpreadsheetId",
                table: "Prospects");

            migrationBuilder.DropColumn(
                name: "ImportId",
                table: "Prospects");

            migrationBuilder.DropColumn(
                name: "SpreadsheetId",
                table: "Prospects");
        }
    }
}
