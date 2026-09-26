using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITPQuotation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRfqRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Rfqs_CustomerId",
                table: "Rfqs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqItems_RfqId",
                table: "RfqItems",
                column: "RfqId");

            migrationBuilder.AddForeignKey(
                name: "FK_RfqItems_Rfqs_RfqId",
                table: "RfqItems",
                column: "RfqId",
                principalTable: "Rfqs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rfqs_Customers_CustomerId",
                table: "Rfqs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RfqItems_Rfqs_RfqId",
                table: "RfqItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Rfqs_Customers_CustomerId",
                table: "Rfqs");

            migrationBuilder.DropIndex(
                name: "IX_Rfqs_CustomerId",
                table: "Rfqs");

            migrationBuilder.DropIndex(
                name: "IX_RfqItems_RfqId",
                table: "RfqItems");
        }
    }
}
