using System.ComponentModel.DataAnnotations;
using ITPQuotation.Api.Models;

namespace ITPQuotation.Api.DTOs.Calculations;

public sealed record InHouseProcessCostCalculationRequest
{
    [Range(typeof(decimal), "0", "1000000000")]
    public decimal SetupTime { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal CycleTime { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000000")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal MachineRatePerHour { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal? LabourCost { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal? ToolCost { get; set; }
}

public sealed record InHouseProcessCostCalculationResponse(
    decimal SetupTime,
    decimal CycleTime,
    decimal Quantity,
    decimal TotalProcessTime,
    decimal MachineRatePerHour,
    decimal MachineCost,
    decimal LabourCost,
    decimal ToolCost,
    decimal TotalProcessCost);

public sealed record OutsourceProcessCostCalculationRequest : IValidatableObject
{
    [Required]
    public string RateType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal Rate { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000000")]
    public decimal? Quantity { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000000")]
    public decimal? TotalWeightKg { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000000")]
    public decimal? Hours { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ProcessCostValues.OutsourceRateTypes.Contains(
                RateType,
                StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                $"RateType must be one of: {string.Join(", ", ProcessCostValues.OutsourceRateTypes)}.",
                [nameof(RateType)]);
            yield break;
        }

        if (RateType.Equals(ProcessCostValues.PerPiece, StringComparison.OrdinalIgnoreCase)
            && Quantity is null)
        {
            yield return RequiredBasis(nameof(Quantity), ProcessCostValues.PerPiece);
        }
        else if (RateType.Equals(ProcessCostValues.PerKg, StringComparison.OrdinalIgnoreCase)
                 && TotalWeightKg is null)
        {
            yield return RequiredBasis(nameof(TotalWeightKg), ProcessCostValues.PerKg);
        }
        else if (RateType.Equals(ProcessCostValues.PerHour, StringComparison.OrdinalIgnoreCase)
                 && Hours is null)
        {
            yield return RequiredBasis(nameof(Hours), ProcessCostValues.PerHour);
        }
    }

    private static ValidationResult RequiredBasis(string member, string rateType) =>
        new($"{member} is required for the {rateType} rate type.", [member]);
}

public sealed record OutsourceProcessCostCalculationResponse(
    string RateType,
    decimal Rate,
    decimal ChargeableAmount,
    string ChargeableUnit,
    decimal TotalProcessCost);

public static class ProcessCostValues
{
    public const string PerPiece = MasterDataValues.PerPiece;
    public const string PerKg = MasterDataValues.PerKg;
    public const string PerHour = MasterDataValues.PerHour;
    public const string Fixed = MasterDataValues.Fixed;

    public static readonly string[] OutsourceRateTypes = [PerPiece, PerKg, PerHour, Fixed];
}
