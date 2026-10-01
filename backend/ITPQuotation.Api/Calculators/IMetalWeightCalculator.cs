using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Calculators;

public interface IMetalWeightCalculator
{
    MetalWeightCalculationResponse Calculate(MetalWeightCalculationRequest request);
}
