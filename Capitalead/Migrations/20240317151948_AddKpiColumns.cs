using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DisabledProspectsCount",
                table: "Spreadsheets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastParsingDate",
                table: "Spreadsheets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LeadsCount",
                table: "Spreadsheets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ProspectsCount",
                table: "Spreadsheets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Spreadsheets",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Lastname = table.Column<string>(type: "text", nullable: false),
                    Firstname = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    MobilePhone = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Spreadsheets_UserId",
                table: "Spreadsheets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Spreadsheets_Users_UserId",
                table: "Spreadsheets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.Sql(@"update ""Spreadsheets""
set ""LastParsingDate"" = (select Max(""ParsingDate"") from ""Prospects"" where ""SpreadsheetId"" = ""Spreadsheets"".""Id"")
where ""LastParsingDate"" is null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Spreadsheets_Users_UserId",
                table: "Spreadsheets");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Spreadsheets_UserId",
                table: "Spreadsheets");

            migrationBuilder.DropColumn(
                name: "DisabledProspectsCount",
                table: "Spreadsheets");

            migrationBuilder.DropColumn(
                name: "LastParsingDate",
                table: "Spreadsheets");

            migrationBuilder.DropColumn(
                name: "LeadsCount",
                table: "Spreadsheets");

            migrationBuilder.DropColumn(
                name: "ProspectsCount",
                table: "Spreadsheets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Spreadsheets");
        }
    }
}
