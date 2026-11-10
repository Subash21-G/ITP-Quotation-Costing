using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITPQuotation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRfqTransportCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TransportCost",
                table: "RfqItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransportCost",
                table: "RfqItems");
        }
    }
}
