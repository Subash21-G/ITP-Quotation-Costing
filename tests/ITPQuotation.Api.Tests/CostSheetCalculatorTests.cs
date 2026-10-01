using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.Models;

namespace ITPQuotation.Api.Tests;

public sealed class CostSheetCalculatorTests
{
    private readonly CostSheetCalculator _calculator = new();

    [Fact]
    public void Calculate_ReturnsCompleteCostBreakdown()
    {
        var result = _calculator.Calculate(
            quantity: 10m,
            overheadPercent: 10m,
            profitPercent: 20m,
            [
                new CostSheetLineAmount(CostSheetValues.RawMaterial, 1000m),
                new CostSheetLineAmount(CostSheetValues.Cnc, 500m),
                new CostSheetLineAmount(CostSheetValues.Packing, 100m)
            ]);

        Assert.Equal(1600m, result.ManufacturingCost);
        Assert.Equal(160m, result.OverheadAmount);
        Assert.Equal(1760m, result.CostAfterOverhead);
        Assert.Equal(352m, result.ProfitAmount);
        Assert.Equal(2112m, result.SellingPrice);
        Assert.Equal(211.2m, result.UnitSellingPrice);
        Assert.Equal(CostSheetValues.Categories.Length, result.CategoryTotals.Count);
        Assert.Equal(1000m, result.CategoryTotals[CostSheetValues.RawMaterial]);
        Assert.Equal(500m, result.CategoryTotals[CostSheetValues.Cnc]);
        Assert.Equal(0m, result.CategoryTotals[CostSheetValues.Transport]);
    }

    [Fact]
    public void Calculate_AggregatesMultipleLinesInTheSameCategory()
    {
        var result = _calculator.Calculate(
            1m,
            0m,
            0m,
            [
                new CostSheetLineAmount(CostSheetValues.Outsource, 125m),
                new CostSheetLineAmount(CostSheetValues.Outsource, 75m)
            ]);

        Assert.Equal(200m, result.CategoryTotals[CostSheetValues.Outsource]);
        Assert.Equal(200m, result.ManufacturingCost);
        Assert.Equal(200m, result.SellingPrice);
    }

    [Fact]
    public void Calculate_DoesNotRoundIntermediateValues()
    {
        const decimal amount = 123.456789m;
        const decimal overhead = 7.6543m;
        const decimal profit = 12.3456m;
        const decimal quantity = 3.125m;

        var result = _calculator.Calculate(
            quantity,
            overhead,
            profit,
            [new CostSheetLineAmount(CostSheetValues.Other, amount)]);

        var expectedOverhead = amount * overhead / 100m;
        var expectedAfterOverhead = amount + expectedOverhead;
        var expectedProfit = expectedAfterOverhead * profit / 100m;
        var expectedSellingPrice = expectedAfterOverhead + expectedProfit;

        Assert.Equal(expectedOverhead, result.OverheadAmount);
        Assert.Equal(expectedProfit, result.ProfitAmount);
        Assert.Equal(expectedSellingPrice / quantity, result.UnitSellingPrice);
    }

    [Fact]
    public void Calculate_WithNonPositiveQuantity_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(0m, 10m, 10m, []));
    }

    [Fact]
    public void Calculate_WithUnsupportedCategory_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(
                1m,
                0m,
                0m,
                [new CostSheetLineAmount("Unknown", 10m)]));
    }
}
