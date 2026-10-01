using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Tests;

public sealed class MetalWeightCalculatorTests
{
    private readonly MetalWeightCalculator _calculator = new();

    [Fact]
    public void Plate_CalculatesWeightAndQuantityTotal()
    {
        var result = _calculator.Calculate(Request(MetalWeightValues.Plate) with
        {
            Length = 1000,
            Width = 100,
            Thickness = 10,
            Quantity = 3
        });

        AssertClose(0.001m, result.CrossSectionArea);
        AssertClose(0.001m, result.VolumePerPiece);
        AssertClose(7.85m, result.WeightPerPieceKg);
        AssertClose(23.55m, result.TotalWeightKg);
    }

    [Fact]
    public void RoundBar_CalculatesUsingCircularArea()
    {
        var result = _calculator.Calculate(Request(MetalWeightValues.RoundBar) with
        {
            Length = 1000,
            Diameter = 100
        });

        AssertClose(0.007853981633974483m, result.CrossSectionArea);
        AssertClose(61.65375582669969m, result.WeightPerPieceKg);
    }

    [Fact]
    public void RoundTube_AcceptsOuterAndInnerDiameters()
    {
        var result = _calculator.Calculate(Request(MetalWeightValues.RoundTube) with
        {
            Length = 1000,
            OuterDiameter = 100,
            InnerDiameter = 80
        });

        AssertClose(0.002827433388230814m, result.CrossSectionArea);
        AssertClose(22.19535209761189m, result.WeightPerPieceKg);
    }

    [Fact]
    public void SquareTube_SubtractsInnerVoid()
    {
        var result = _calculator.Calculate(Request(MetalWeightValues.SquareTube) with
        {
            Length = 1000,
            Side = 50,
            Thickness = 5
        });

        AssertClose(0.0009m, result.CrossSectionArea);
        AssertClose(7.065m, result.WeightPerPieceKg);
    }

    [Fact]
    public void RectangularTube_SubtractsInnerVoid()
    {
        var result = _calculator.Calculate(Request(MetalWeightValues.RectangularTube) with
        {
            Length = 1000,
            Width = 100,
            Height = 50,
            Thickness = 5
        });

        AssertClose(0.0014m, result.CrossSectionArea);
        AssertClose(10.99m, result.WeightPerPieceKg);
    }

    [Fact]
    public void HexBar_UsesAcrossFlatsConvention()
    {
        var result = _calculator.Calculate(Request(MetalWeightValues.HexBar) with
        {
            Length = 1000,
            AcrossFlats = 50
        });

        AssertClose(0.002165063509461097m, result.CrossSectionArea);
        AssertClose(16.99574854926961m, result.WeightPerPieceKg);
    }

    [Theory]
    [InlineData("mm", 1000)]
    [InlineData("cm", 100)]
    [InlineData("m", 1)]
    [InlineData("inch", 39.37007874015748)]
    public void Units_NormalizeToSameSiResult(string unit, double length)
    {
        var dimension = (decimal)length;
        var result = _calculator.Calculate(Request(MetalWeightValues.SquareBar) with
        {
            Unit = unit,
            Length = dimension,
            Side = dimension / 10m
        });

        AssertClose(78.50m, result.WeightPerPieceKg, 0.000000001m);
    }

    [Fact]
    public void WeightToLength_ReturnsLengthInRequestedUnit()
    {
        var result = _calculator.Calculate(Request(MetalWeightValues.Plate) with
        {
            Mode = MetalWeightValues.WeightToLength,
            Length = null,
            TargetWeightPerPieceKg = 7.85m,
            Width = 100,
            Thickness = 10
        });

        AssertClose(1000m, result.RequiredLength!.Value);
        Assert.Equal("mm", result.RequiredLengthUnit);
        AssertClose(0.001m, result.VolumePerPiece);
    }

    [Fact]
    public void ImpossibleTubeGeometry_IsRejected()
    {
        var request = Request(MetalWeightValues.SquareTube) with
        {
            Length = 1000,
            Side = 50,
            Thickness = 25
        };

        Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
            () => _calculator.Calculate(request));
    }

    private static MetalWeightCalculationRequest Request(string shape) =>
        new()
        {
            Shape = shape,
            Unit = MetalWeightValues.Millimetre,
            Mode = MetalWeightValues.LengthToWeight,
            DensityKgM3 = 7850,
            Quantity = 1
        };

    private static void AssertClose(
        decimal expected,
        decimal actual,
        decimal tolerance = 0.000000000001m) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
}
