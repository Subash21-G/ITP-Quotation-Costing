using System.ComponentModel.DataAnnotations;
using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Calculators;

public sealed class MetalWeightCalculator : IMetalWeightCalculator
{
    private const decimal Pi = 3.1415926535897932384626433833m;
    private const decimal SquareRootOfThree = 1.7320508075688772935274463415m;

    public MetalWeightCalculationResponse Calculate(MetalWeightCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var unit = LengthUnitConverter.CanonicalUnit(request.Unit);
        var shape = CanonicalValue(request.Shape, MetalWeightValues.Shapes, nameof(request.Shape));
        var mode = CanonicalValue(request.Mode, MetalWeightValues.Modes, nameof(request.Mode));
        var areaM2 = CalculateAreaM2(request, shape, unit);

        decimal lengthM;
        decimal weightPerPieceKg;

        if (mode == MetalWeightValues.LengthToWeight)
        {
            lengthM = LengthUnitConverter.ToMetres(request.Length!.Value, unit);
            weightPerPieceKg = areaM2 * lengthM * request.DensityKgM3;
        }
        else
        {
            weightPerPieceKg = request.TargetWeightPerPieceKg!.Value;
            lengthM = weightPerPieceKg / (areaM2 * request.DensityKgM3);
        }

        var volumePerPieceM3 = areaM2 * lengthM;

        return new MetalWeightCalculationResponse(
            shape,
            mode,
            unit,
            request.DensityKgM3,
            areaM2,
            volumePerPieceM3,
            weightPerPieceKg,
            request.Quantity,
            weightPerPieceKg * request.Quantity,
            mode == MetalWeightValues.WeightToLength
                ? LengthUnitConverter.FromMetres(lengthM, unit)
                : null,
            mode == MetalWeightValues.WeightToLength ? unit : null);
    }

    private static decimal CalculateAreaM2(
        MetalWeightCalculationRequest request,
        string shape,
        string unit)
    {
        decimal Metres(decimal? value) => LengthUnitConverter.ToMetres(value!.Value, unit);

        return shape switch
        {
            MetalWeightValues.RoundBar => Pi * Square(Metres(request.Diameter)) / 4m,
            MetalWeightValues.SquareBar => Square(Metres(request.Side)),
            MetalWeightValues.Plate or MetalWeightValues.FlatBar =>
                Metres(request.Width) * Metres(request.Thickness),
            MetalWeightValues.RoundTube => RoundTubeArea(request, unit),
            MetalWeightValues.SquareTube => SquareTubeArea(request, unit),
            MetalWeightValues.RectangularTube => RectangularTubeArea(request, unit),
            MetalWeightValues.HexBar =>
                SquareRootOfThree / 2m * Square(Metres(request.AcrossFlats)),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Shape))
        };
    }

    private static decimal RoundTubeArea(MetalWeightCalculationRequest request, string unit)
    {
        var outerDiameterM = LengthUnitConverter.ToMetres(request.OuterDiameter!.Value, unit);
        var innerDiameterM = request.InnerDiameter is not null
            ? LengthUnitConverter.ToMetres(request.InnerDiameter.Value, unit)
            : outerDiameterM - 2m * LengthUnitConverter.ToMetres(request.Thickness!.Value, unit);

        return Pi / 4m * (Square(outerDiameterM) - Square(innerDiameterM));
    }

    private static decimal SquareTubeArea(MetalWeightCalculationRequest request, string unit)
    {
        var outerSideM = LengthUnitConverter.ToMetres(request.Side!.Value, unit);
        var innerSideM = outerSideM
            - 2m * LengthUnitConverter.ToMetres(request.Thickness!.Value, unit);

        return Square(outerSideM) - Square(innerSideM);
    }

    private static decimal RectangularTubeArea(
        MetalWeightCalculationRequest request,
        string unit)
    {
        var outerWidthM = LengthUnitConverter.ToMetres(request.Width!.Value, unit);
        var outerHeightM = LengthUnitConverter.ToMetres(request.Height!.Value, unit);
        var twiceThicknessM =
            2m * LengthUnitConverter.ToMetres(request.Thickness!.Value, unit);

        return outerWidthM * outerHeightM
            - (outerWidthM - twiceThicknessM) * (outerHeightM - twiceThicknessM);
    }

    private static decimal Square(decimal value) => value * value;

    private static string CanonicalValue(string value, string[] supported, string parameterName) =>
        supported.FirstOrDefault(item => item.Equals(value, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentOutOfRangeException(parameterName, value, "Unsupported value.");

    private static void Validate(MetalWeightCalculationRequest request)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true))
        {
            throw new ValidationException(string.Join(" ", results.Select(result => result.ErrorMessage)));
        }
    }
}
