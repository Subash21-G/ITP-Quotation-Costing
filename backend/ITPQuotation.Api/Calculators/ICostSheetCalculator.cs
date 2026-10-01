namespace ITPQuotation.Api.Calculators;

public interface ICostSheetCalculator
{
    CostSheetCalculationResult Calculate(
        decimal quantity,
        decimal overheadPercent,
        decimal profitPercent,
        IEnumerable<CostSheetLineAmount> lines);
}

public sealed record CostSheetLineAmount(string Category, decimal Amount);

public sealed record CostSheetCalculationResult(
    IReadOnlyDictionary<string, decimal> CategoryTotals,
    decimal ManufacturingCost,
    decimal OverheadAmount,
    decimal CostAfterOverhead,
    decimal ProfitAmount,
    decimal SellingPrice,
    decimal UnitSellingPrice);
