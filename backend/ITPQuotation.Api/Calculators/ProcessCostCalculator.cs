using System.ComponentModel.DataAnnotations;
using ITPQuotation.Api.DTOs.Calculations;

namespace ITPQuotation.Api.Calculators;

public sealed class ProcessCostCalculator : IProcessCostCalculator
{
    public InHouseProcessCostCalculationResponse CalculateInHouse(
        InHouseProcessCostCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var totalProcessTime = request.SetupTime + request.CycleTime * request.Quantity;
        var machineCost = totalProcessTime * request.MachineRatePerHour;
        var labourCost = request.LabourCost.GetValueOrDefault();
        var toolCost = request.ToolCost.GetValueOrDefault();

        return new InHouseProcessCostCalculationResponse(
            request.SetupTime,
            request.CycleTime,
            request.Quantity,
            totalProcessTime,
            request.MachineRatePerHour,
            machineCost,
            labourCost,
            toolCost,
            machineCost + labourCost + toolCost);
    }

    public OutsourceProcessCostCalculationResponse CalculateOutsource(
        OutsourceProcessCostCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var rateType = ProcessCostValues.OutsourceRateTypes.First(
            value => value.Equals(request.RateType, StringComparison.OrdinalIgnoreCase));

        var (chargeableAmount, chargeableUnit) = rateType switch
        {
            ProcessCostValues.PerPiece => (request.Quantity!.Value, "piece"),
            ProcessCostValues.PerKg => (request.TotalWeightKg!.Value, "kg"),
            ProcessCostValues.PerHour => (request.Hours!.Value, "hour"),
            ProcessCostValues.Fixed => (1m, "fixed"),
            _ => throw new ArgumentOutOfRangeException(nameof(request.RateType))
        };

        return new OutsourceProcessCostCalculationResponse(
            rateType,
            request.Rate,
            chargeableAmount,
            chargeableUnit,
            request.Rate * chargeableAmount);
    }

    private static void Validate(object request)
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
