using System.ComponentModel.DataAnnotations;
using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Tests;

public sealed class RawMaterialCostCalculatorTests
{
    private readonly RawMaterialCostCalculator _calculator = new();

    [Fact]
    public void Calculate_WithoutScrap_ReturnsGrossCostAsNetCost()
    {
        var result = _calculator.Calculate(new RawMaterialCostCalculationRequest
        {
            WeightPerPiece = 12.5m,
            Quantity = 8m,
            MaterialRatePerKg = 80.75m
        });

        Assert.Equal(100m, result.TotalRawWeight);
        Assert.Equal(8075m, result.GrossMaterialCost);
        Assert.Equal(0m, result.ScrapRecovery);
        Assert.Equal(8075m, result.NetMaterialCost);
    }

    [Fact]
    public void Calculate_WithScrap_DeductsRecoveryFromGrossCost()
    {
        var result = _calculator.Calculate(new RawMaterialCostCalculationRequest
        {
            WeightPerPiece = 5m,
            Quantity = 10m,
            MaterialRatePerKg = 100m,
            ScrapWeight = 8m,
            ScrapRecoveryRate = 25m
        });

        Assert.Equal(50m, result.TotalRawWeight);
        Assert.Equal(5000m, result.GrossMaterialCost);
        Assert.Equal(200m, result.ScrapRecovery);
        Assert.Equal(4800m, result.NetMaterialCost);
    }

    [Fact]
    public void Calculate_DoesNotRoundIntermediateValues()
    {
        const decimal weightPerPiece = 1.234567m;
        const decimal quantity = 3.5m;
        const decimal rate = 78.98765m;

        var result = _calculator.Calculate(new RawMaterialCostCalculationRequest
        {
            WeightPerPiece = weightPerPiece,
            Quantity = quantity,
            MaterialRatePerKg = rate
        });

        Assert.Equal(weightPerPiece * quantity, result.TotalRawWeight);
        Assert.Equal(weightPerPiece * quantity * rate, result.GrossMaterialCost);
        Assert.Equal(weightPerPiece * quantity * rate, result.NetMaterialCost);
    }

    [Fact]
    public void Calculate_WithOnlyScrapWeight_DefaultsRecoveryRateToZero()
    {
        var result = _calculator.Calculate(new RawMaterialCostCalculationRequest
        {
            WeightPerPiece = 10m,
            Quantity = 2m,
            MaterialRatePerKg = 50m,
            ScrapWeight = 5m
        });

        Assert.Equal(0m, result.ScrapRecoveryRate);
        Assert.Equal(0m, result.ScrapRecovery);
        Assert.Equal(1000m, result.NetMaterialCost);
    }

    [Fact]
    public void Calculate_WhenScrapRecoveryExceedsGrossCost_IsRejected()
    {
        var request = new RawMaterialCostCalculationRequest
        {
            WeightPerPiece = 1m,
            Quantity = 1m,
            MaterialRatePerKg = 10m,
            ScrapWeight = 2m,
            ScrapRecoveryRate = 10m
        };

        Assert.Throws<ValidationException>(() => _calculator.Calculate(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calculate_WithNonPositiveWeight_IsRejected(double weight)
    {
        var request = new RawMaterialCostCalculationRequest
        {
            WeightPerPiece = (decimal)weight,
            Quantity = 1m,
            MaterialRatePerKg = 10m
        };

        Assert.Throws<ValidationException>(() => _calculator.Calculate(request));
    }
}
