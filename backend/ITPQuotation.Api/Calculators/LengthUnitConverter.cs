using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Calculators;

public static class LengthUnitConverter
{
    private const decimal MillimetresPerMetre = 1000m;
    private const decimal CentimetresPerMetre = 100m;
    private const decimal InchesPerMetre = 39.370078740157480314960629921m;

    public static decimal ToMetres(decimal value, string unit) =>
        CanonicalUnit(unit) switch
        {
            MetalWeightValues.Millimetre => value / MillimetresPerMetre,
            MetalWeightValues.Centimetre => value / CentimetresPerMetre,
            MetalWeightValues.Metre => value,
            MetalWeightValues.Inch => value / InchesPerMetre,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported length unit.")
        };

    public static decimal FromMetres(decimal value, string unit) =>
        CanonicalUnit(unit) switch
        {
            MetalWeightValues.Millimetre => value * MillimetresPerMetre,
            MetalWeightValues.Centimetre => value * CentimetresPerMetre,
            MetalWeightValues.Metre => value,
            MetalWeightValues.Inch => value * InchesPerMetre,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported length unit.")
        };

    public static string CanonicalUnit(string unit) =>
        MetalWeightValues.Units.FirstOrDefault(
            value => value.Equals(unit, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported length unit.");
}
