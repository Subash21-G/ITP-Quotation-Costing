using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.CostSheets;
using ITPQuotation.Api.Models;
using ITPQuotation.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Tests;

public sealed class CostSheetServiceTests
{
    [Fact]
    public async Task Create_PersistsLinesAndReturnsCalculatedBreakdown()
    {
        await using var context = CreateContext();
        var item = await AddRfqItemAsync(context);
        var service = CreateService(context);

        var result = await service.CreateAsync(
            CreateRequest(item.Id),
            CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.Equal(item.Id, result.RfqItemId);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(CostSheetValues.Cnc, result.Lines[1].Category);
        Assert.Equal(1500m, result.ManufacturingCost);
        Assert.Equal(1980m, result.SellingPrice);
        Assert.Equal(1, await context.CostSheets.CountAsync());
        Assert.Equal(2, await context.CostSheetLines.CountAsync());
    }

    [Fact]
    public async Task Update_ReplacesLinesAndRecalculatesTotals()
    {
        await using var context = CreateContext();
        var item = await AddRfqItemAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(
            CreateRequest(item.Id),
            CancellationToken.None);

        var updated = await service.UpdateAsync(
            created.Id,
            new CostSheetUpdateRequest
            {
                Quantity = 5m,
                OverheadPercent = 0m,
                ProfitPercent = 10m,
                Lines =
                [
                    new CostSheetLineRequest
                    {
                        Sequence = 1,
                        Category = CostSheetValues.Packing,
                        Amount = 200m
                    }
                ]
            },
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Single(updated.Lines);
        Assert.Equal(200m, updated.ManufacturingCost);
        Assert.Equal(220m, updated.SellingPrice);
        Assert.Equal(44m, updated.UnitSellingPrice);
        Assert.Equal(1, await context.CostSheetLines.CountAsync());
    }

    [Fact]
    public async Task Create_WhenRfqItemAlreadyHasCostSheet_ReturnsConflict()
    {
        await using var context = CreateContext();
        var item = await AddRfqItemAsync(context);
        var service = CreateService(context);
        var request = CreateRequest(item.Id);
        await service.CreateAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<CostSheetConflictException>(
            () => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithUnknownRfqItem_IsRejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<CostSheetValidationException>(
            () => service.CreateAsync(CreateRequest(999), CancellationToken.None));
    }

    private static CostSheetService CreateService(ApplicationDbContext context) =>
        new(context, new CostSheetCalculator());

    private static CostSheetCreateRequest CreateRequest(int rfqItemId) =>
        new()
        {
            RfqItemId = rfqItemId,
            Quantity = 10m,
            OverheadPercent = 10m,
            ProfitPercent = 20m,
            Lines =
            [
                new CostSheetLineRequest
                {
                    Sequence = 1,
                    Category = CostSheetValues.RawMaterial,
                    Amount = 1000m
                },
                new CostSheetLineRequest
                {
                    Sequence = 2,
                    Category = "cnc",
                    Description = " Machining ",
                    Amount = 500m
                }
            ]
        };

    private static async Task<RfqItem> AddRfqItemAsync(ApplicationDbContext context)
    {
        var item = new RfqItem
        {
            RfqId = 42,
            MaterialNo = "COST-SHEET-MATERIAL",
            Quantity = 10m
        };
        context.RfqItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
