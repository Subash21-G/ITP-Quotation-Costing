using System.ComponentModel.DataAnnotations;
using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Tests;

public sealed class ProcessCostCalculatorTests
{
    private readonly ProcessCostCalculator _calculator = new();

    [Fact]
    public void InHouse_CalculatesTimeMachineAndAdditionalCosts()
    {
        var result = _calculator.CalculateInHouse(new InHouseProcessCostCalculationRequest
        {
            SetupTime = 2m,
            CycleTime = 0.5m,
            Quantity = 10m,
            MachineRatePerHour = 1000m,
            LabourCost = 500m,
            ToolCost = 250m
        });

        Assert.Equal(7m, result.TotalProcessTime);
        Assert.Equal(7000m, result.MachineCost);
        Assert.Equal(7750m, result.TotalProcessCost);
    }

    [Fact]
    public void InHouse_OptionalCostsDefaultToZero()
    {
        var result = _calculator.CalculateInHouse(new InHouseProcessCostCalculationRequest
        {
            SetupTime = 0.25m,
            CycleTime = 0.1m,
            Quantity = 5m,
            MachineRatePerHour = 800m
        });

        Assert.Equal(0.75m, result.TotalProcessTime);
        Assert.Equal(600m, result.MachineCost);
        Assert.Equal(0m, result.LabourCost);
        Assert.Equal(0m, result.ToolCost);
        Assert.Equal(600m, result.TotalProcessCost);
    }

    [Fact]
    public void InHouse_DoesNotRoundIntermediateValues()
    {
        const decimal setupTime = 0.123456m;
        const decimal cycleTime = 0.234567m;
        const decimal quantity = 3.5m;
        const decimal rate = 789.12345m;

        var result = _calculator.CalculateInHouse(new InHouseProcessCostCalculationRequest
        {
            SetupTime = setupTime,
            CycleTime = cycleTime,
            Quantity = quantity,
            MachineRatePerHour = rate
        });

        var expectedTime = setupTime + cycleTime * quantity;
        Assert.Equal(expectedTime, result.TotalProcessTime);
        Assert.Equal(expectedTime * rate, result.MachineCost);
    }

    [Fact]
    public void InHouse_WithZeroQuantity_IsRejected()
    {
        var request = new InHouseProcessCostCalculationRequest
        {
            CycleTime = 1m,
            Quantity = 0m,
            MachineRatePerHour = 100m
        };

        Assert.Throws<ValidationException>(() => _calculator.CalculateInHouse(request));
    }

    [Theory]
    [InlineData("PerPiece", 50, 10, 500, "piece")]
    [InlineData("PerKg", 80, 12.5, 1000, "kg")]
    [InlineData("PerHour", 400, 2.5, 1000, "hour")]
    [InlineData("Fixed", 1500, 1, 1500, "fixed")]
    public void Outsource_CalculatesSupportedRateTypes(
        string rateType,
        double rate,
        double basis,
        double expectedCost,
        string expectedUnit)
    {
        var request = new OutsourceProcessCostCalculationRequest
        {
            RateType = rateType,
            Rate = (decimal)rate,
            Quantity = rateType == ProcessCostValues.PerPiece ? (decimal)basis : null,
            TotalWeightKg = rateType == ProcessCostValues.PerKg ? (decimal)basis : null,
            Hours = rateType == ProcessCostValues.PerHour ? (decimal)basis : null
        };

        var result = _calculator.CalculateOutsource(request);

        Assert.Equal((decimal)expectedCost, result.TotalProcessCost);
        Assert.Equal(expectedUnit, result.ChargeableUnit);
        Assert.Equal((decimal)basis, result.ChargeableAmount);
    }

    [Theory]
    [InlineData("PerPiece")]
    [InlineData("PerKg")]
    [InlineData("PerHour")]
    public void Outsource_WhenRequiredBasisIsMissing_IsRejected(string rateType)
    {
        var request = new OutsourceProcessCostCalculationRequest
        {
            RateType = rateType,
            Rate = 100m
        };

        Assert.Throws<ValidationException>(() => _calculator.CalculateOutsource(request));
    }

    [Fact]
    public void Outsource_WithUnsupportedRateType_IsRejected()
    {
        var request = new OutsourceProcessCostCalculationRequest
        {
            RateType = "PerMetre",
            Rate = 100m
        };

        Assert.Throws<ValidationException>(() => _calculator.CalculateOutsource(request));
    }
}
