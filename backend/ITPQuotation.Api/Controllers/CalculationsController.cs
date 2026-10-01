using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.DTOs.Calculations;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/calculations")]
public sealed class CalculationsController(
    IMetalWeightCalculator metalWeightCalculator,
    IRawMaterialCostCalculator rawMaterialCostCalculator,
    IProcessCostCalculator processCostCalculator) : ControllerBase
{
    [HttpPost("metal-weight")]
    public ActionResult<MetalWeightCalculationResponse> CalculateMetalWeight(
        MetalWeightCalculationRequest request) =>
        Ok(metalWeightCalculator.Calculate(request));

    [HttpPost("raw-material-cost")]
    public ActionResult<RawMaterialCostCalculationResponse> CalculateRawMaterialCost(
        RawMaterialCostCalculationRequest request) =>
        Ok(rawMaterialCostCalculator.Calculate(request));

    [HttpPost("process-cost/in-house")]
    public ActionResult<InHouseProcessCostCalculationResponse> CalculateInHouseProcessCost(
        InHouseProcessCostCalculationRequest request) =>
        Ok(processCostCalculator.CalculateInHouse(request));

    [HttpPost("process-cost/outsource")]
    public ActionResult<OutsourceProcessCostCalculationResponse> CalculateOutsourceProcessCost(
        OutsourceProcessCostCalculationRequest request) =>
        Ok(processCostCalculator.CalculateOutsource(request));
}
