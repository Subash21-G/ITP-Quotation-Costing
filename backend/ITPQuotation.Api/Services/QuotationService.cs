using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.Quotations;
using ITPQuotation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Services;

public sealed class QuotationService(
    ApplicationDbContext context,
    ICostSheetCalculator costSheetCalculator)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<QuotationResponse>> ListAsync(
        int? customerId,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = Query(asTracking: false);
        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = CanonicalStatus(status);
            query = query.Where(x => x.Status == normalizedStatus);
        }

        var quotations = await query
            .OrderByDescending(x => x.QuotationDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
        return quotations.Select(ToResponse).ToList();
    }

    public async Task<QuotationResponse?> GetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var quotation = await Query(asTracking: false).SingleOrDefaultAsync(
            x => x.Id == id,
            cancellationToken);
        return quotation is null ? null : ToResponse(quotation);
    }

    public async Task<QuotationResponse> CreateAsync(
        QuotationCreateRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var quotationNumber = request.QuotationNumber.Trim();
        if (await context.Quotations.AnyAsync(
                x => x.QuotationNumber == quotationNumber,
                cancellationToken))
        {
            throw new QuotationConflictException(
                $"Quotation number '{quotationNumber}' already exists.");
        }

        var (customer, rfq) = await LoadCustomerAndRfqAsync(
            request.CustomerId,
            request.RfqId,
            cancellationToken);
        var now = DateTime.UtcNow;
        var quotation = new Quotation
        {
            QuotationNumber = quotationNumber,
            CustomerId = customer.Id,
            Customer = customer,
            RfqId = rfq.Id,
            Rfq = rfq,
            Revision = 0,
            CreatedDate = now,
            UpdatedDate = now
        };
        ApplyCommercial(quotation, request);

        foreach (var item in await BuildItemsAsync(
                     rfq.Id,
                     request.Items,
                     cancellationToken))
        {
            quotation.Items.Add(item);
        }

        AddRevisionSnapshot(quotation, now);
        context.Quotations.Add(quotation);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new QuotationConflictException(
                $"Quotation number '{quotationNumber}' already exists.",
                exception);
        }

        return ToResponse(quotation);
    }

    public async Task<QuotationResponse?> UpdateAsync(
        int id,
        QuotationUpdateRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var quotation = await Query(asTracking: true).SingleOrDefaultAsync(
            x => x.Id == id,
            cancellationToken);
        if (quotation is null)
        {
            return null;
        }

        var replacementItems = await BuildItemsAsync(
            quotation.RfqId,
            request.Items,
            cancellationToken);
        context.QuotationItems.RemoveRange(quotation.Items);
        quotation.Items.Clear();
        foreach (var item in replacementItems)
        {
            quotation.Items.Add(item);
        }

        quotation.Revision++;
        quotation.UpdatedDate = DateTime.UtcNow;
        ApplyCommercial(quotation, request);
        AddRevisionSnapshot(quotation, quotation.UpdatedDate);
        await context.SaveChangesAsync(cancellationToken);
        return ToResponse(quotation);
    }

    public async Task<IReadOnlyList<QuotationRevisionSummaryResponse>?> ListRevisionsAsync(
        int quotationId,
        CancellationToken cancellationToken)
    {
        if (!await context.Quotations.AnyAsync(
                x => x.Id == quotationId,
                cancellationToken))
        {
            return null;
        }

        return await context.QuotationRevisions
            .AsNoTracking()
            .Where(x => x.QuotationId == quotationId)
            .OrderBy(x => x.Revision)
            .Select(x => new QuotationRevisionSummaryResponse(
                x.Id,
                x.Revision,
                x.CreatedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<QuotationRevisionResponse?> GetRevisionAsync(
        int quotationId,
        int revision,
        CancellationToken cancellationToken)
    {
        var entity = await context.QuotationRevisions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.QuotationId == quotationId && x.Revision == revision,
                cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return new QuotationRevisionResponse(
            entity.Id,
            entity.QuotationId,
            entity.Revision,
            entity.CreatedDate,
            Deserialize<QuotationSnapshot>(entity.SnapshotJson));
    }

    private async Task<List<QuotationItem>> BuildItemsAsync(
        int rfqId,
        IReadOnlyList<QuotationItemWriteRequest> requests,
        CancellationToken cancellationToken)
    {
        var result = new List<QuotationItem>(requests.Count);
        foreach (var request in requests)
        {
            var rfqItem = await context.RfqItems
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == request.RfqItemId && x.RfqId == rfqId,
                    cancellationToken)
                ?? throw new QuotationValidationException(
                    $"RFQ item {request.RfqItemId} does not belong to the quotation RFQ.");

            var costSheet = await context.CostSheets
                .AsNoTracking()
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(
                    x => x.RfqItemId == request.RfqItemId,
                    cancellationToken)
                ?? throw new QuotationValidationException(
                    $"RFQ item {request.RfqItemId} does not have a cost sheet.");

            var material = await context.MaterialMasters
                .AsNoTracking()
                .Include(x => x.MetalMaterial)
                .Include(x => x.Routings)
                    .ThenInclude(x => x.Process)
                .Include(x => x.Routings)
                    .ThenInclude(x => x.Vendor)
                .SingleOrDefaultAsync(
                    x => x.MaterialNo == rfqItem.MaterialNo,
                    cancellationToken);

            var calculation = costSheetCalculator.Calculate(
                costSheet.Quantity,
                costSheet.OverheadPercent,
                costSheet.ProfitPercent,
                costSheet.Lines.Select(
                    x => new CostSheetLineAmount(x.Category, x.Amount)));
            var costSnapshot = new QuotationCostSnapshot(
                costSheet.Lines
                    .OrderBy(x => x.Sequence)
                    .Select(x => new QuotationCostLineSnapshot(
                        x.Sequence,
                        x.Category,
                        x.Description,
                        x.Amount))
                    .ToList(),
                calculation.CategoryTotals,
                calculation.ManufacturingCost,
                costSheet.OverheadPercent,
                calculation.OverheadAmount,
                calculation.CostAfterOverhead,
                costSheet.ProfitPercent,
                calculation.ProfitAmount,
                calculation.SellingPrice,
                calculation.UnitSellingPrice);
            var processRoute = BuildProcessRoute(request, material);
            var weightPerPiece = request.WeightPerPieceKg;
            var totalWeight = request.TotalWeightKg
                ?? (weightPerPiece.HasValue
                    ? weightPerPiece.Value * costSheet.Quantity
                    : null);
            var unitPrice = request.UnitPriceOverride ?? calculation.UnitSellingPrice;

            result.Add(new QuotationItem
            {
                RfqItemId = rfqItem.Id,
                MaterialNo = rfqItem.MaterialNo,
                Description = rfqItem.Description,
                DrawingNo = rfqItem.DrawingNo,
                Quantity = costSheet.Quantity,
                MaterialRatePerKg =
                    request.MaterialRatePerKg ?? material?.MetalMaterial?.DefaultRatePerKg,
                DensityKgM3 =
                    request.DensityKgM3 ?? material?.MetalMaterial?.DensityKgM3,
                RawMaterialShape =
                    Clean(request.RawMaterialShape) ?? material?.RawMaterialShape,
                RawMaterialDimensions =
                    Clean(request.RawMaterialDimensions) ?? material?.RawMaterialSize,
                WeightPerPieceKg = weightPerPiece,
                TotalWeightKg = totalWeight,
                ManufacturingCost = calculation.ManufacturingCost,
                OverheadPercent = costSheet.OverheadPercent,
                OverheadAmount = calculation.OverheadAmount,
                ProfitPercent = costSheet.ProfitPercent,
                ProfitAmount = calculation.ProfitAmount,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * costSheet.Quantity,
                CostBreakdownJson = JsonSerializer.Serialize(costSnapshot, JsonOptions),
                ProcessRouteJson = JsonSerializer.Serialize(processRoute, JsonOptions)
            });
        }

        return result;
    }

    private static IReadOnlyList<QuotationProcessSnapshot> BuildProcessRoute(
        QuotationItemWriteRequest request,
        MaterialMaster? material)
    {
        if (request.ProcessRoute.Count > 0)
        {
            return request.ProcessRoute
                .OrderBy(x => x.Sequence)
                .Select(x => x with
                {
                    ProcessName = x.ProcessName.Trim(),
                    ProcessType = x.ProcessType.Trim(),
                    VendorName = Clean(x.VendorName),
                    RateType = Clean(x.RateType)
                })
                .ToList();
        }

        return material?.Routings
            .OrderBy(x => x.Sequence)
            .Select(x => new QuotationProcessSnapshot
            {
                Sequence = x.Sequence,
                ProcessName = x.Process.ProcessName,
                ProcessType = x.ProcessType,
                VendorName = x.Vendor?.VendorName,
                RateType = x.ProcessType == MasterDataValues.Outsource
                    ? MasterDataValues.PerPiece
                    : MasterDataValues.PerHour,
                Rate = x.ProcessType == MasterDataValues.Outsource
                    ? x.RatePerPiece
                    : x.MachineRate,
                MachineRate = x.MachineRate,
                SetupTimeHours = x.SetupTimeHours,
                CycleTimeHours = x.CycleTimeHours,
                Cost = 0m
            })
            .ToList() ?? [];
    }

    private void AddRevisionSnapshot(Quotation quotation, DateTime createdDate)
    {
        quotation.Revisions.Add(new QuotationRevision
        {
            Revision = quotation.Revision,
            CreatedDate = createdDate,
            SnapshotJson = JsonSerializer.Serialize(
                ToSnapshot(quotation, includeItemIds: false),
                JsonOptions)
        });
    }

    private QuotationResponse ToResponse(Quotation quotation)
    {
        var snapshot = ToSnapshot(quotation, includeItemIds: true);
        return new QuotationResponse
        {
            Id = quotation.Id,
            PoTrackerPurchaseOrderId = quotation.PoTrackerPurchaseOrderId,
            PoTrackerPoNumber = quotation.PoTrackerPoNumber,
            PoTrackerExportedRevision = quotation.PoTrackerExportedRevision,
            QuotationNumber = snapshot.QuotationNumber,
            CustomerId = snapshot.CustomerId,
            CustomerName = snapshot.CustomerName,
            RfqId = snapshot.RfqId,
            RfqNumber = snapshot.RfqNumber,
            Revision = snapshot.Revision,
            QuotationDate = snapshot.QuotationDate,
            ValidUntil = snapshot.ValidUntil,
            PaymentTerms = snapshot.PaymentTerms,
            DeliveryTerms = snapshot.DeliveryTerms,
            DeliveryTime = snapshot.DeliveryTime,
            FreightTerms = snapshot.FreightTerms,
            TaxNotes = snapshot.TaxNotes,
            Status = snapshot.Status,
            Items = snapshot.Items,
            TotalPrice = snapshot.TotalPrice,
            CreatedDate = quotation.CreatedDate,
            UpdatedDate = quotation.UpdatedDate
        };
    }

    private QuotationSnapshot ToSnapshot(Quotation quotation, bool includeItemIds)
    {
        var items = quotation.Items
            .OrderBy(x => x.Id)
            .Select(x => ToItemSnapshot(x, includeItemIds))
            .ToList();
        return new QuotationSnapshot
        {
            QuotationNumber = quotation.QuotationNumber,
            CustomerId = quotation.CustomerId,
            CustomerName = quotation.Customer.CustomerName,
            RfqId = quotation.RfqId,
            RfqNumber = quotation.Rfq.RfqNumber,
            Revision = quotation.Revision,
            QuotationDate = quotation.QuotationDate,
            ValidUntil = quotation.ValidUntil,
            PaymentTerms = quotation.PaymentTerms,
            DeliveryTerms = quotation.DeliveryTerms,
            DeliveryTime = quotation.DeliveryTime,
            FreightTerms = quotation.FreightTerms,
            TaxNotes = quotation.TaxNotes,
            Status = quotation.Status,
            Items = items,
            TotalPrice = items.Sum(x => x.TotalPrice)
        };
    }

    private static QuotationItemSnapshot ToItemSnapshot(
        QuotationItem item,
        bool includeItemId) =>
        new()
        {
            Id = includeItemId ? item.Id : null,
            RfqItemId = item.RfqItemId,
            MaterialNo = item.MaterialNo,
            Description = item.Description,
            DrawingNo = item.DrawingNo,
            Quantity = item.Quantity,
            MaterialRatePerKg = item.MaterialRatePerKg,
            DensityKgM3 = item.DensityKgM3,
            RawMaterialShape = item.RawMaterialShape,
            RawMaterialDimensions = item.RawMaterialDimensions,
            WeightPerPieceKg = item.WeightPerPieceKg,
            TotalWeightKg = item.TotalWeightKg,
            ProcessRoute = Deserialize<List<QuotationProcessSnapshot>>(item.ProcessRouteJson),
            CostBreakdown = Deserialize<QuotationCostSnapshot>(item.CostBreakdownJson),
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice
        };

    private IQueryable<Quotation> Query(bool asTracking)
    {
        var query = context.Quotations
            .Include(x => x.Customer)
            .Include(x => x.Rfq)
            .Include(x => x.Items)
            .Include(x => x.Revisions)
            .AsSplitQuery()
            .AsQueryable();
        return asTracking ? query : query.AsNoTracking();
    }

    private async Task<(Customer Customer, Rfq Rfq)> LoadCustomerAndRfqAsync(
        int customerId,
        int rfqId,
        CancellationToken cancellationToken)
    {
        var customer = await context.Customers.SingleOrDefaultAsync(
            x => x.Id == customerId,
            cancellationToken)
            ?? throw new QuotationValidationException("Customer does not exist.");
        var rfq = await context.Rfqs.SingleOrDefaultAsync(
            x => x.Id == rfqId,
            cancellationToken)
            ?? throw new QuotationValidationException("RFQ does not exist.");
        if (rfq.CustomerId != customerId)
        {
            throw new QuotationValidationException(
                "RFQ does not belong to the selected customer.");
        }

        return (customer, rfq);
    }

    private static void ApplyCommercial(Quotation quotation, QuotationWriteRequest request)
    {
        quotation.QuotationDate = request.QuotationDate;
        quotation.ValidUntil = request.ValidUntil;
        quotation.PaymentTerms = Clean(request.PaymentTerms);
        quotation.DeliveryTerms = Clean(request.DeliveryTerms);
        quotation.DeliveryTime = Clean(request.DeliveryTime);
        quotation.FreightTerms = Clean(request.FreightTerms);
        quotation.TaxNotes = Clean(request.TaxNotes);
        quotation.Status = CanonicalStatus(request.Status);
    }

    private static void ValidateRequest(QuotationWriteRequest request)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true))
        {
            throw new QuotationValidationException(
                string.Join(" ", results.Select(x => x.ErrorMessage)));
        }
    }

    private static string CanonicalStatus(string status) =>
        QuotationValues.Statuses.FirstOrDefault(
            value => value.Equals(status, StringComparison.OrdinalIgnoreCase))
        ?? throw new QuotationValidationException($"Unsupported quotation status '{status}'.");

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new InvalidOperationException("Stored quotation snapshot is invalid.");

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
