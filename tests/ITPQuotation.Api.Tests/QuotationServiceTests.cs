using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.Quotations;
using ITPQuotation.Api.Models;
using ITPQuotation.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Tests;

public sealed class QuotationServiceTests
{
    [Fact]
    public async Task Create_CapturesCostEngineeringAndRouteSnapshot()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);
        var service = CreateService(context);

        var result = await service.CreateAsync(
            CreateRequest(seed),
            CancellationToken.None);

        Assert.Equal(0, result.Revision);
        Assert.Equal(1320m, result.TotalPrice);
        var item = Assert.Single(result.Items);
        Assert.Equal(80m, item.MaterialRatePerKg);
        Assert.Equal(7850m, item.DensityKgM3);
        Assert.Equal(12.5m, item.WeightPerPieceKg);
        Assert.Equal(125m, item.TotalWeightKg);
        Assert.Equal(1000m, item.CostBreakdown.ManufacturingCost);
        var route = Assert.Single(item.ProcessRoute);
        Assert.Equal("CNC", route.ProcessName);
        Assert.Equal(900m, route.MachineRate);
        Assert.Equal(1, await context.QuotationRevisions.CountAsync());
    }

    [Fact]
    public async Task Revise_PreservesOldSnapshotWhenMasterAndCostsChange()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(
            CreateRequest(seed),
            CancellationToken.None);

        seed.Metal.DefaultRatePerKg = 999m;
        seed.Metal.DensityKgM3 = 8000m;
        seed.CostLine.Amount = 2000m;
        await context.SaveChangesAsync();

        var revised = await service.UpdateAsync(
            created.Id,
            new QuotationUpdateRequest
            {
                QuotationDate = new DateTime(2026, 9, 29),
                ValidUntil = new DateTime(2026, 10, 29),
                PaymentTerms = "45 days",
                Status = "Ready",
                Items =
                [
                    new QuotationItemWriteRequest
                    {
                        RfqItemId = seed.RfqItem.Id,
                        WeightPerPieceKg = 12.5m
                    }
                ]
            },
            CancellationToken.None);

        Assert.NotNull(revised);
        Assert.Equal(1, revised.Revision);
        Assert.Equal(2640m, revised.TotalPrice);
        Assert.Equal(999m, Assert.Single(revised.Items).MaterialRatePerKg);

        var revision0 = await service.GetRevisionAsync(
            created.Id,
            0,
            CancellationToken.None);
        var revision1 = await service.GetRevisionAsync(
            created.Id,
            1,
            CancellationToken.None);

        Assert.NotNull(revision0);
        Assert.NotNull(revision1);
        Assert.Equal(1320m, revision0.Snapshot.TotalPrice);
        Assert.Equal(80m, Assert.Single(revision0.Snapshot.Items).MaterialRatePerKg);
        Assert.Equal(7850m, Assert.Single(revision0.Snapshot.Items).DensityKgM3);
        Assert.Equal(2640m, revision1.Snapshot.TotalPrice);
        Assert.Equal(999m, Assert.Single(revision1.Snapshot.Items).MaterialRatePerKg);
        Assert.Equal(2, await context.QuotationRevisions.CountAsync());
    }

    [Fact]
    public async Task Create_WithDuplicateQuotationNumber_ReturnsConflict()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);
        var service = CreateService(context);
        var request = CreateRequest(seed);
        await service.CreateAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<QuotationConflictException>(
            () => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithoutCostSheet_IsRejected()
    {
        await using var context = CreateContext();
        var seed = await SeedAsync(context);
        context.CostSheets.Remove(seed.CostSheet);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await Assert.ThrowsAsync<QuotationValidationException>(
            () => service.CreateAsync(CreateRequest(seed), CancellationToken.None));
    }

    private static QuotationService CreateService(ApplicationDbContext context) =>
        new(context, new CostSheetCalculator());

    private static QuotationCreateRequest CreateRequest(SeedData seed) =>
        new()
        {
            QuotationNumber = $"Q-{Guid.NewGuid():N}",
            CustomerId = seed.Customer.Id,
            RfqId = seed.Rfq.Id,
            QuotationDate = new DateTime(2026, 9, 28),
            ValidUntil = new DateTime(2026, 10, 28),
            PaymentTerms = "30 days",
            DeliveryTerms = "Ex works",
            Status = "Draft",
            Items =
            [
                new QuotationItemWriteRequest
                {
                    RfqItemId = seed.RfqItem.Id,
                    WeightPerPieceKg = 12.5m
                }
            ]
        };

    private static async Task<SeedData> SeedAsync(ApplicationDbContext context)
    {
        var customer = new Customer { CustomerName = "Quotation Customer" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        var rfq = new Rfq
        {
            CustomerId = customer.Id,
            RfqNumber = "RFQ-Q",
            Status = "Draft"
        };
        context.Rfqs.Add(rfq);
        await context.SaveChangesAsync();
        var rfqItem = new RfqItem
        {
            RfqId = rfq.Id,
            MaterialNo = "MAT-Q",
            Description = "Quoted part",
            DrawingNo = "DRG-Q",
            Quantity = 10m
        };
        context.RfqItems.Add(rfqItem);

        var metal = new MetalMaterial
        {
            Name = "Carbon Steel",
            DensityKgM3 = 7850m,
            DefaultRatePerKg = 80m
        };
        var process = new ProcessMaster
        {
            ProcessName = "CNC",
            DefaultProcessType = MasterDataValues.InHouse,
            DefaultMachineRate = 900m
        };
        context.AddRange(metal, process);
        await context.SaveChangesAsync();
        var material = new MaterialMaster
        {
            MaterialNo = rfqItem.MaterialNo,
            MetalMaterialId = metal.Id,
            RawMaterialShape = "Plate",
            RawMaterialSize = "100 x 50 x 10"
        };
        context.MaterialMasters.Add(material);
        await context.SaveChangesAsync();
        context.MaterialRoutings.Add(new MaterialRouting
        {
            MaterialMasterId = material.Id,
            Sequence = 1,
            ProcessId = process.Id,
            ProcessType = MasterDataValues.InHouse,
            MachineRate = 900m,
            SetupTimeHours = 1m,
            CycleTimeHours = 0.25m
        });

        var costSheet = new CostSheet
        {
            RfqItemId = rfqItem.Id,
            Quantity = 10m,
            OverheadPercent = 10m,
            ProfitPercent = 20m
        };
        var costLine = new CostSheetLine
        {
            Sequence = 1,
            Category = CostSheetValues.RawMaterial,
            Amount = 1000m
        };
        costSheet.Lines.Add(costLine);
        context.CostSheets.Add(costSheet);
        await context.SaveChangesAsync();
        return new SeedData(customer, rfq, rfqItem, metal, costSheet, costLine);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed record SeedData(
        Customer Customer,
        Rfq Rfq,
        RfqItem RfqItem,
        MetalMaterial Metal,
        CostSheet CostSheet,
        CostSheetLine CostLine);
}
