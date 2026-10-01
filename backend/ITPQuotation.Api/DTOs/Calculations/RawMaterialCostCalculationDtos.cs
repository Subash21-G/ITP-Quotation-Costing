using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.Calculations;

public sealed record RawMaterialCostCalculationRequest : IValidatableObject
{
    [Range(typeof(decimal), "0.000001", "1000000000")]
    public decimal WeightPerPiece { get; set; }

    [Range(typeof(decimal), "0.000001", "1000000000")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal MaterialRatePerKg { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal? ScrapWeight { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal? ScrapRecoveryRate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var grossMaterialCost = WeightPerPiece * Quantity * MaterialRatePerKg;
        var scrapRecovery =
            ScrapWeight.GetValueOrDefault() * ScrapRecoveryRate.GetValueOrDefault();

        if (scrapRecovery > grossMaterialCost)
        {
            yield return new ValidationResult(
                "Scrap recovery cannot exceed gross material cost.",
                [nameof(ScrapWeight), nameof(ScrapRecoveryRate)]);
        }
    }
}

public sealed record RawMaterialCostCalculationResponse(
    decimal WeightPerPiece,
    decimal Quantity,
    decimal MaterialRatePerKg,
    decimal TotalRawWeight,
    decimal GrossMaterialCost,
    decimal ScrapWeight,
    decimal ScrapRecoveryRate,
    decimal ScrapRecovery,
    decimal NetMaterialCost);
