using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Calculators;

public interface IProcessCostCalculator
{
    InHouseProcessCostCalculationResponse CalculateInHouse(
        InHouseProcessCostCalculationRequest request);

    OutsourceProcessCostCalculationResponse CalculateOutsource(
        OutsourceProcessCostCalculationRequest request);
}
