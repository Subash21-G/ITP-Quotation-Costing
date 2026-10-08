using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITPQuotation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPoTrackerExportReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PoTrackerExportedRevision",
                table: "Quotations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoTrackerPoNumber",
                table: "Quotations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PoTrackerPurchaseOrderId",
                table: "Quotations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoTrackerExportedRevision",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "PoTrackerPoNumber",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "PoTrackerPurchaseOrderId",
                table: "Quotations");
        }
    }
}
