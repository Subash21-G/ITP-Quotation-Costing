using System.ComponentModel.DataAnnotations;
using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.CostSheets;
using ITPQuotation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Services;

public sealed class CostSheetService(
    ApplicationDbContext context,
    ICostSheetCalculator calculator)
{
    public async Task<IReadOnlyList<CostSheetResponse>> ListAsync(
        int? rfqItemId,
        CancellationToken cancellationToken)
    {
        var query = Query();
        if (rfqItemId.HasValue)
        {
            query = query.Where(x => x.RfqItemId == rfqItemId.Value);
        }

        var costSheets = await query
            .OrderByDescending(x => x.UpdatedDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return costSheets.Select(ToResponse).ToList();
    }

    public async Task<CostSheetResponse?> GetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var costSheet = await Query().SingleOrDefaultAsync(
            x => x.Id == id,
            cancellationToken);
        return costSheet is null ? null : ToResponse(costSheet);
    }

    public async Task<CostSheetResponse?> GetByRfqItemAsync(
        int rfqItemId,
        CancellationToken cancellationToken)
    {
        var costSheet = await Query().SingleOrDefaultAsync(
            x => x.RfqItemId == rfqItemId,
            cancellationToken);
        return costSheet is null ? null : ToResponse(costSheet);
    }

    public async Task<CostSheetResponse> CreateAsync(
        CostSheetCreateRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var rfqItem = await context.RfqItems.SingleOrDefaultAsync(
            x => x.Id == request.RfqItemId,
            cancellationToken)
            ?? throw new CostSheetValidationException("RFQ item does not exist.");

        if (await context.CostSheets.AnyAsync(
                x => x.RfqItemId == request.RfqItemId,
                cancellationToken))
        {
            throw new CostSheetConflictException(
                "A cost sheet already exists for this RFQ item.");
        }

        var now = DateTime.UtcNow;
        var costSheet = new CostSheet
        {
            RfqItemId = request.RfqItemId,
            RfqItem = rfqItem,
            CreatedDate = now,
            UpdatedDate = now
        };
        Apply(costSheet, request);
        context.CostSheets.Add(costSheet);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new CostSheetConflictException(
                "A cost sheet already exists for this RFQ item.",
                exception);
        }

        return ToResponse(costSheet);
    }

    public async Task<CostSheetResponse?> UpdateAsync(
        int id,
        CostSheetUpdateRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var costSheet = await context.CostSheets
            .Include(x => x.RfqItem)
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (costSheet is null)
        {
            return null;
        }

        context.CostSheetLines.RemoveRange(costSheet.Lines);
        costSheet.Lines.Clear();
        Apply(costSheet, request);
        costSheet.UpdatedDate = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(costSheet);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var costSheet = await context.CostSheets.FindAsync([id], cancellationToken);
        if (costSheet is null)
        {
            return false;
        }

        context.CostSheets.Remove(costSheet);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<CostSheet> Query() =>
        context.CostSheets
            .AsNoTracking()
            .Include(x => x.RfqItem)
            .Include(x => x.Lines);

    private CostSheetResponse ToResponse(CostSheet costSheet)
    {
        var orderedLines = costSheet.Lines.OrderBy(x => x.Sequence).ToList();
        var calculation = calculator.Calculate(
            costSheet.Quantity,
            costSheet.OverheadPercent,
            costSheet.ProfitPercent,
            orderedLines.Select(x => new CostSheetLineAmount(x.Category, x.Amount)));

        return new CostSheetResponse(
            costSheet.Id,
            costSheet.RfqItemId,
            costSheet.RfqItem.RfqId,
            costSheet.Quantity,
            costSheet.OverheadPercent,
            costSheet.ProfitPercent,
            orderedLines.Select(x => new CostSheetLineResponse(
                x.Id,
                x.Sequence,
                x.Category,
                x.Description,
                x.Amount)).ToList(),
            calculation.CategoryTotals,
            calculation.ManufacturingCost,
            calculation.OverheadAmount,
            calculation.CostAfterOverhead,
            calculation.ProfitAmount,
            calculation.SellingPrice,
            calculation.UnitSellingPrice,
            costSheet.CreatedDate,
            costSheet.UpdatedDate);
    }

    private static void Apply(CostSheet costSheet, CostSheetWriteRequest request)
    {
        costSheet.Quantity = request.Quantity;
        costSheet.OverheadPercent = request.OverheadPercent;
        costSheet.ProfitPercent = request.ProfitPercent;

        foreach (var requestLine in request.Lines.OrderBy(x => x.Sequence))
        {
            var category = CostSheetValues.Categories.First(
                value => value.Equals(
                    requestLine.Category,
                    StringComparison.OrdinalIgnoreCase));
            costSheet.Lines.Add(new CostSheetLine
            {
                Sequence = requestLine.Sequence,
                Category = category,
                Description = Clean(requestLine.Description),
                Amount = requestLine.Amount
            });
        }
    }

    private static void ValidateRequest(CostSheetWriteRequest request)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true))
        {
            throw new CostSheetValidationException(
                string.Join(" ", results.Select(result => result.ErrorMessage)));
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
