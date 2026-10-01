using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITPQuotation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRfqPdfReadAndFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dimensions",
                table: "RfqItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "RfqItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dimensions",
                table: "RfqItems");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "RfqItems");
        }
    }
}
