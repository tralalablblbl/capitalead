using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class AddDbFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DbFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Exported = table.Column<bool>(type: "boolean", nullable: false),
                    ReadyForExport = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CiviliteColumn = table.Column<int>(type: "integer", nullable: false),
                    FirstnameColumn = table.Column<int>(type: "integer", nullable: false),
                    LastnameColumn = table.Column<int>(type: "integer", nullable: false),
                    PhoneColumn = table.Column<int>(type: "integer", nullable: false),
                    ZipcodeColumn = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbProspects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetName = table.Column<string>(type: "text", nullable: false),
                    RowNumber = table.Column<long>(type: "bigint", nullable: false),
                    Civilite = table.Column<string>(type: "text", nullable: true),
                    Firstname = table.Column<string>(type: "text", nullable: true),
                    Lastname = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Zipcode = table.Column<string>(type: "text", nullable: true),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbProspects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbProspects_DbFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "DbFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbProspects_FileId",
                table: "DbProspects",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbProspects");

            migrationBuilder.DropTable(
                name: "DbFiles");
        }
    }
}
