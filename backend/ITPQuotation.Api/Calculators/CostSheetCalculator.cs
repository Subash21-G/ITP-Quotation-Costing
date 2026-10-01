using ITPQuotation.Api.Models;

namespace ITPQuotation.Api.Calculators;

public sealed class CostSheetCalculator : ICostSheetCalculator
{
    public CostSheetCalculationResult Calculate(
        decimal quantity,
        decimal overheadPercent,
        decimal profitPercent,
        IEnumerable<CostSheetLineAmount> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (overheadPercent < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(overheadPercent),
                "Overhead percentage cannot be negative.");
        }

        if (profitPercent < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(profitPercent),
                "Profit percentage cannot be negative.");
        }

        var categoryTotals = CostSheetValues.Categories.ToDictionary(
            category => category,
            _ => 0m,
            StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (line.Amount < 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lines),
                    "Cost line amounts cannot be negative.");
            }

            var category = CostSheetValues.Categories.FirstOrDefault(
                value => value.Equals(line.Category, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentOutOfRangeException(
                    nameof(lines),
                    $"Unsupported cost category '{line.Category}'.");
            categoryTotals[category] += line.Amount;
        }

        var manufacturingCost = categoryTotals.Values.Sum();
        var overheadAmount = manufacturingCost * overheadPercent / 100m;
        var costAfterOverhead = manufacturingCost + overheadAmount;
        var profitAmount = costAfterOverhead * profitPercent / 100m;
        var sellingPrice = costAfterOverhead + profitAmount;

        return new CostSheetCalculationResult(
            categoryTotals,
            manufacturingCost,
            overheadAmount,
            costAfterOverhead,
            profitAmount,
            sellingPrice,
            sellingPrice / quantity);
    }
}
