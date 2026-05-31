using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNameColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Firstname",
                table: "DbProspects");

            migrationBuilder.RenameColumn(
                name: "Lastname",
                table: "DbProspects",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "NameColumn",
                table: "DbFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameColumn",
                table: "DbFiles");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "DbProspects",
                newName: "Lastname");

            migrationBuilder.AddColumn<string>(
                name: "Firstname",
                table: "DbProspects",
                type: "text",
                nullable: true);
        }
    }
}
