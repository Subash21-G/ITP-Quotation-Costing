using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITPQuotation.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMastersheetFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetalMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DensityKgM3 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DefaultRatePerKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetalMaterials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DefaultProcessType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefaultMachineRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DrawingCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DrawingVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FinishSize = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetalMaterialId = table.Column<int>(type: "int", nullable: true),
                    RawMaterialShape = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RawMaterialSize = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialMasters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialMasters_MetalMaterials_MetalMaterialId",
                        column: x => x.MetalMaterialId,
                        principalTable: "MetalMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorProcessRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    RateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorProcessRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorProcessRates_ProcessMasters_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "ProcessMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorProcessRates_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialRoutings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialMasterId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    ProcessType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    MachineRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SetupTimeHours = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CycleTimeHours = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RatePerPiece = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRoutings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRoutings_MaterialMasters_MaterialMasterId",
                        column: x => x.MaterialMasterId,
                        principalTable: "MaterialMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialRoutings_ProcessMasters_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "ProcessMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialRoutings_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMasters_MaterialNo",
                table: "MaterialMasters",
                column: "MaterialNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMasters_MetalMaterialId",
                table: "MaterialMasters",
                column: "MetalMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRoutings_MaterialMasterId_Sequence",
                table: "MaterialRoutings",
                columns: new[] { "MaterialMasterId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRoutings_ProcessId",
                table: "MaterialRoutings",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRoutings_VendorId",
                table: "MaterialRoutings",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_MetalMaterials_Name_Grade",
                table: "MetalMaterials",
                columns: new[] { "Name", "Grade" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessMasters_ProcessName",
                table: "ProcessMasters",
                column: "ProcessName");

            migrationBuilder.CreateIndex(
                name: "IX_VendorProcessRates_ProcessId",
                table: "VendorProcessRates",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorProcessRates_VendorId_ProcessId_EffectiveFrom",
                table: "VendorProcessRates",
                columns: new[] { "VendorId", "ProcessId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorName",
                table: "Vendors",
                column: "VendorName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialRoutings");

            migrationBuilder.DropTable(
                name: "VendorProcessRates");

            migrationBuilder.DropTable(
                name: "MaterialMasters");

            migrationBuilder.DropTable(
                name: "ProcessMasters");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "MetalMaterials");
        }
    }
}
