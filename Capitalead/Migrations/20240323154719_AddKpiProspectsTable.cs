using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitalead.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiProspectsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Disabled",
                table: "Prospects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "LeadId",
                table: "Prospects",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProspectId",
                table: "Prospects",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prospects_ProspectId",
                table: "Prospects",
                column: "ProspectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prospects_ProspectId",
                table: "Prospects");

            migrationBuilder.DropColumn(
                name: "Disabled",
                table: "Prospects");

            migrationBuilder.DropColumn(
                name: "LeadId",
                table: "Prospects");

            migrationBuilder.DropColumn(
                name: "ProspectId",
                table: "Prospects");
        }
    }
}
