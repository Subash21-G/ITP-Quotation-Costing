using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.Calculations;

public sealed record MetalWeightCalculationRequest : IValidatableObject
{
    [Required]
    public string Shape { get; set; } = string.Empty;

    [Required]
    public string Unit { get; set; } = string.Empty;

    [Required]
    public string Mode { get; set; } = MetalWeightValues.LengthToWeight;

    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal DensityKgM3 { get; set; }

    [Range(1, 1000000)]
    public int Quantity { get; set; } = 1;

    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal? Length { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000000000000000")]
    public decimal? TargetWeightPerPieceKg { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal? Diameter { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal? OuterDiameter { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal? InnerDiameter { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal? Side { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal? Width { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal? Height { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal? Thickness { get; set; }

    /// <summary>The distance between two opposite flat faces of a regular hexagon.</summary>
    [Range(typeof(decimal), "0.000001", "1000000")]
    public decimal? AcrossFlats { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!MetalWeightValues.Shapes.Contains(Shape, StringComparer.OrdinalIgnoreCase))
        {
            yield return Error(
                $"Shape must be one of: {string.Join(", ", MetalWeightValues.Shapes)}.",
                nameof(Shape));
        }

        if (!MetalWeightValues.Units.Contains(Unit, StringComparer.OrdinalIgnoreCase))
        {
            yield return Error(
                $"Unit must be one of: {string.Join(", ", MetalWeightValues.Units)}.",
                nameof(Unit));
        }

        if (!MetalWeightValues.Modes.Contains(Mode, StringComparer.OrdinalIgnoreCase))
        {
            yield return Error(
                $"Mode must be one of: {string.Join(", ", MetalWeightValues.Modes)}.",
                nameof(Mode));
        }
        else if (Mode.Equals(MetalWeightValues.LengthToWeight, StringComparison.OrdinalIgnoreCase)
                 && Length is null)
        {
            yield return Error("Length is required in LengthToWeight mode.", nameof(Length));
        }
        else if (Mode.Equals(MetalWeightValues.WeightToLength, StringComparison.OrdinalIgnoreCase)
                 && TargetWeightPerPieceKg is null)
        {
            yield return Error(
                "TargetWeightPerPieceKg is required in WeightToLength mode.",
                nameof(TargetWeightPerPieceKg));
        }

        foreach (var result in ValidateShape())
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateShape()
    {
        if (Shape.Equals(MetalWeightValues.RoundBar, StringComparison.OrdinalIgnoreCase))
        {
            if (Diameter is null)
            {
                yield return Error("Diameter is required for RoundBar.", nameof(Diameter));
            }
        }
        else if (Shape.Equals(MetalWeightValues.SquareBar, StringComparison.OrdinalIgnoreCase))
        {
            if (Side is null)
            {
                yield return Error("Side is required for SquareBar.", nameof(Side));
            }
        }
        else if (Shape.Equals(MetalWeightValues.Plate, StringComparison.OrdinalIgnoreCase)
                 || Shape.Equals(MetalWeightValues.FlatBar, StringComparison.OrdinalIgnoreCase))
        {
            if (Width is null)
            {
                yield return Error("Width is required for Plate and FlatBar.", nameof(Width));
            }

            if (Thickness is null)
            {
                yield return Error("Thickness is required for Plate and FlatBar.", nameof(Thickness));
            }
        }
        else if (Shape.Equals(MetalWeightValues.RoundTube, StringComparison.OrdinalIgnoreCase))
        {
            if (OuterDiameter is null)
            {
                yield return Error("OuterDiameter is required for RoundTube.", nameof(OuterDiameter));
            }

            if (InnerDiameter is null == (Thickness is null))
            {
                yield return Error(
                    "Provide exactly one of InnerDiameter or Thickness for RoundTube.",
                    nameof(InnerDiameter), nameof(Thickness));
            }

            if (OuterDiameter is not null && InnerDiameter >= OuterDiameter)
            {
                yield return Error(
                    "InnerDiameter must be smaller than OuterDiameter.",
                    nameof(InnerDiameter));
            }

            if (OuterDiameter is not null && Thickness * 2 >= OuterDiameter)
            {
                yield return Error(
                    "Twice the wall Thickness must be smaller than OuterDiameter.",
                    nameof(Thickness));
            }
        }
        else if (Shape.Equals(MetalWeightValues.SquareTube, StringComparison.OrdinalIgnoreCase))
        {
            if (Side is null)
            {
                yield return Error("Side is required for SquareTube.", nameof(Side));
            }

            if (Thickness is null)
            {
                yield return Error("Thickness is required for SquareTube.", nameof(Thickness));
            }

            if (Side is not null && Thickness * 2 >= Side)
            {
                yield return Error(
                    "Twice the wall Thickness must be smaller than Side.",
                    nameof(Thickness));
            }
        }
        else if (Shape.Equals(MetalWeightValues.RectangularTube, StringComparison.OrdinalIgnoreCase))
        {
            if (Width is null)
            {
                yield return Error("Width is required for RectangularTube.", nameof(Width));
            }

            if (Height is null)
            {
                yield return Error("Height is required for RectangularTube.", nameof(Height));
            }

            if (Thickness is null)
            {
                yield return Error("Thickness is required for RectangularTube.", nameof(Thickness));
            }

            if ((Width is not null && Thickness * 2 >= Width)
                || (Height is not null && Thickness * 2 >= Height))
            {
                yield return Error(
                    "Twice the wall Thickness must be smaller than both Width and Height.",
                    nameof(Thickness));
            }
        }
        else if (Shape.Equals(MetalWeightValues.HexBar, StringComparison.OrdinalIgnoreCase)
                 && AcrossFlats is null)
        {
            yield return Error(
                "AcrossFlats is required for HexBar; hex dimensions use the across-flats convention.",
                nameof(AcrossFlats));
        }
    }

    private static ValidationResult Error(string message, params string[] members) =>
        new(message, members);
}

public sealed record MetalWeightCalculationResponse(
    string Shape,
    string Mode,
    string Unit,
    decimal DensityKgM3,
    decimal CrossSectionArea,
    decimal VolumePerPiece,
    decimal WeightPerPieceKg,
    int Quantity,
    decimal TotalWeightKg,
    decimal? RequiredLength,
    string? RequiredLengthUnit);

public static class MetalWeightValues
{
    public const string RoundBar = "RoundBar";
    public const string SquareBar = "SquareBar";
    public const string Plate = "Plate";
    public const string FlatBar = "FlatBar";
    public const string RoundTube = "RoundTube";
    public const string SquareTube = "SquareTube";
    public const string RectangularTube = "RectangularTube";
    public const string HexBar = "HexBar";

    public const string Millimetre = "mm";
    public const string Centimetre = "cm";
    public const string Metre = "m";
    public const string Inch = "inch";

    public const string LengthToWeight = "LengthToWeight";
    public const string WeightToLength = "WeightToLength";

    public static readonly string[] Shapes =
    [
        RoundBar,
        SquareBar,
        Plate,
        FlatBar,
        RoundTube,
        SquareTube,
        RectangularTube,
        HexBar
    ];

    public static readonly string[] Units = [Millimetre, Centimetre, Metre, Inch];
    public static readonly string[] Modes = [LengthToWeight, WeightToLength];
}
