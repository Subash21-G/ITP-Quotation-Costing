using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Calculators;

public interface IRawMaterialCostCalculator
{
    RawMaterialCostCalculationResponse Calculate(RawMaterialCostCalculationRequest request);
}
