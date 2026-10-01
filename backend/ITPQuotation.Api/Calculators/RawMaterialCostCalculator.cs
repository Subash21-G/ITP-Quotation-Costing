using System.ComponentModel.DataAnnotations;
using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Calculators;

public sealed class RawMaterialCostCalculator : IRawMaterialCostCalculator
{
    public RawMaterialCostCalculationResponse Calculate(RawMaterialCostCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var totalRawWeight = request.WeightPerPiece * request.Quantity;
        var grossMaterialCost = totalRawWeight * request.MaterialRatePerKg;
        var scrapWeight = request.ScrapWeight.GetValueOrDefault();
        var scrapRecoveryRate = request.ScrapRecoveryRate.GetValueOrDefault();
        var scrapRecovery = scrapWeight * scrapRecoveryRate;
        var netMaterialCost = grossMaterialCost - scrapRecovery;

        return new RawMaterialCostCalculationResponse(
            request.WeightPerPiece,
            request.Quantity,
            request.MaterialRatePerKg,
            totalRawWeight,
            grossMaterialCost,
            scrapWeight,
            scrapRecoveryRate,
            scrapRecovery,
            netMaterialCost);
    }

    private static void Validate(RawMaterialCostCalculationRequest request)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true))
        {
            throw new ValidationException(
                string.Join(" ", results.Select(result => result.ErrorMessage)));
        }
    }
}
