using ITPQuotation.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ITPQuotation.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261107000000_AddMaterialSpecificGravities")]
public sealed class AddMaterialSpecificGravities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Specific gravity is relative to water; the master stores density in kg/m³.
        migrationBuilder.Sql("""
            UPDATE m SET DensityKgM3 = v.DensityKgM3, UpdatedDate = GETUTCDATE()
            FROM MetalMaterials AS m
            JOIN (VALUES
                ('MS', 7850), ('SS', 7900), ('Copper', 9000),
                ('Cast Iron', 9000), ('Brass', 9000), ('Nylon', 2000),
                ('Aluminium', 2700)
            ) AS v(Name, DensityKgM3) ON m.Name = v.Name
            WHERE m.Grade IS NULL;

            INSERT INTO MetalMaterials (Name, Grade, DensityKgM3, DefaultRatePerKg, IsActive, CreatedDate, UpdatedDate)
            SELECT v.Name, NULL, v.DensityKgM3, 0, 1, GETUTCDATE(), GETUTCDATE()
            FROM (VALUES
                ('MS', 7850), ('SS', 7900), ('Copper', 9000),
                ('Cast Iron', 9000), ('Brass', 9000), ('Nylon', 2000),
                ('Aluminium', 2700)
            ) AS v(Name, DensityKgM3)
            WHERE NOT EXISTS (
                SELECT 1 FROM MetalMaterials AS m
                WHERE m.Name = v.Name AND m.Grade IS NULL
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep master data that may already be referenced by materials or quotations.
    }
}
