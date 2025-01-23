using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFileColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CiviliteColumn",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "FirstnameColumn",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "LastnameColumn",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "NameColumn",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "PhoneColumn",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "ZipcodeColumn",
                table: "DbFiles");

            migrationBuilder.AddColumn<int[]>(
                name: "CiviliteColumns",
                table: "DbFiles",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int[]>(
                name: "FirstnameColumns",
                table: "DbFiles",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int[]>(
                name: "LastnameColumns",
                table: "DbFiles",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int[]>(
                name: "NameColumns",
                table: "DbFiles",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int[]>(
                name: "PhoneColumns",
                table: "DbFiles",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int[]>(
                name: "ZipcodeColumns",
                table: "DbFiles",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CiviliteColumns",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "FirstnameColumns",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "LastnameColumns",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "NameColumns",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "PhoneColumns",
                table: "DbFiles");

            migrationBuilder.DropColumn(
                name: "ZipcodeColumns",
                table: "DbFiles");

            migrationBuilder.AddColumn<int>(
                name: "CiviliteColumn",
                table: "DbFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FirstnameColumn",
                table: "DbFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastnameColumn",
                table: "DbFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NameColumn",
                table: "DbFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhoneColumn",
                table: "DbFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ZipcodeColumn",
                table: "DbFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
