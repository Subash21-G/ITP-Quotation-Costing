using System.ComponentModel.DataAnnotations;
using ITPQuotation.Api.Models;

namespace ITPQuotation.Api.DTOs.Quotations;

public abstract record QuotationWriteRequest : IValidatableObject
{
    public DateTime QuotationDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ValidUntil { get; set; }

    [StringLength(500)]
    public string? PaymentTerms { get; set; }

    [StringLength(500)]
    public string? DeliveryTerms { get; set; }

    [StringLength(200)]
    public string? DeliveryTime { get; set; }

    [StringLength(500)]
    public string? FreightTerms { get; set; }

    [StringLength(1000)]
    public string? TaxNotes { get; set; }

    [Required, StringLength(30)]
    public string Status { get; set; } = QuotationValues.Draft;

    [Required, MinLength(1)]
    public List<QuotationItemWriteRequest> Items { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!QuotationValues.Statuses.Contains(Status, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                $"Status must be one of: {string.Join(", ", QuotationValues.Statuses)}.",
                [nameof(Status)]);
        }

        if (ValidUntil.HasValue && ValidUntil.Value.Date < QuotationDate.Date)
        {
            yield return new ValidationResult(
                "ValidUntil cannot be earlier than QuotationDate.",
                [nameof(ValidUntil)]);
        }

        if (Items is null)
        {
            yield break;
        }

        if (Items.Count > 100)
        {
            yield return new ValidationResult(
                "A quotation cannot contain more than 100 items.",
                [nameof(Items)]);
        }

        var itemIds = new HashSet<int>();
        for (var index = 0; index < Items.Count; index++)
        {
            if (!itemIds.Add(Items[index].RfqItemId))
            {
                yield return new ValidationResult(
                    $"RFQ item {Items[index].RfqItemId} is duplicated.",
                    [$"{nameof(Items)}[{index}].{nameof(QuotationItemWriteRequest.RfqItemId)}"]);
            }
        }
    }
}

public sealed record QuotationCreateRequest : QuotationWriteRequest
{
    [Required, StringLength(100)]
    public string QuotationNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Range(1, int.MaxValue)]
    public int RfqId { get; set; }
}

public sealed record QuotationUpdateRequest : QuotationWriteRequest;

public sealed record QuotationItemWriteRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int RfqItemId { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999")]
    public decimal? MaterialRatePerKg { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999999.999999")]
    public decimal? DensityKgM3 { get; set; }

    [StringLength(50)]
    public string? RawMaterialShape { get; set; }

    [StringLength(500)]
    public string? RawMaterialDimensions { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999999.999999")]
    public decimal? WeightPerPieceKg { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999999.999999")]
    public decimal? TotalWeightKg { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999")]
    public decimal? UnitPriceOverride { get; set; }

    [Required]
    public List<QuotationProcessSnapshot> ProcessRoute { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProcessRoute is null)
        {
            yield break;
        }

        var sequences = new HashSet<int>();
        for (var index = 0; index < ProcessRoute.Count; index++)
        {
            var route = ProcessRoute[index];
            if (!sequences.Add(route.Sequence))
            {
                yield return new ValidationResult(
                    $"Process route sequence {route.Sequence} is duplicated.",
                    [$"{nameof(ProcessRoute)}[{index}].{nameof(route.Sequence)}"]);
            }

            if (route.ProcessType != MasterDataValues.InHouse
                && route.ProcessType != MasterDataValues.Outsource)
            {
                yield return new ValidationResult(
                    $"ProcessType must be {MasterDataValues.InHouse} or " +
                    $"{MasterDataValues.Outsource}.",
                    [$"{nameof(ProcessRoute)}[{index}].{nameof(route.ProcessType)}"]);
            }

            if (route.RateType is not null
                && route.RateType != MasterDataValues.PerPiece
                && route.RateType != MasterDataValues.PerKg
                && route.RateType != MasterDataValues.PerHour
                && route.RateType != MasterDataValues.Fixed)
            {
                yield return new ValidationResult(
                    "RateType is not supported.",
                    [$"{nameof(ProcessRoute)}[{index}].{nameof(route.RateType)}"]);
            }
        }
    }
}

public sealed record QuotationProcessSnapshot
{
    [Range(1, int.MaxValue)]
    public int Sequence { get; set; }

    [Required, StringLength(100)]
    public string ProcessName { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string ProcessType { get; set; } = string.Empty;

    [StringLength(200)]
    public string? VendorName { get; set; }

    [StringLength(20)]
    public string? RateType { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999")]
    public decimal Rate { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999")]
    public decimal MachineRate { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999")]
    public decimal SetupTimeHours { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999")]
    public decimal CycleTimeHours { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999")]
    public decimal Cost { get; set; }
}

public sealed record QuotationCostLineSnapshot(
    int Sequence,
    string Category,
    string? Description,
    decimal Amount);

public sealed record QuotationCostSnapshot(
    IReadOnlyList<QuotationCostLineSnapshot> Lines,
    IReadOnlyDictionary<string, decimal> CategoryTotals,
    decimal ManufacturingCost,
    decimal OverheadPercent,
    decimal OverheadAmount,
    decimal CostAfterOverhead,
    decimal ProfitPercent,
    decimal ProfitAmount,
    decimal SellingPrice,
    decimal UnitSellingPrice);

public record QuotationItemSnapshot
{
    public int? Id { get; init; }
    public int RfqItemId { get; init; }
    public string MaterialNo { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? DrawingNo { get; init; }
    public decimal Quantity { get; init; }
    public decimal? MaterialRatePerKg { get; init; }
    public decimal? DensityKgM3 { get; init; }
    public string? RawMaterialShape { get; init; }
    public string? RawMaterialDimensions { get; init; }
    public decimal? WeightPerPieceKg { get; init; }
    public decimal? TotalWeightKg { get; init; }
    public IReadOnlyList<QuotationProcessSnapshot> ProcessRoute { get; init; } = [];
    public QuotationCostSnapshot CostBreakdown { get; init; } = null!;
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}

public record QuotationSnapshot
{
    public string QuotationNumber { get; init; } = string.Empty;
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public int RfqId { get; init; }
    public string RfqNumber { get; init; } = string.Empty;
    public int Revision { get; init; }
    public DateTime QuotationDate { get; init; }
    public DateTime? ValidUntil { get; init; }
    public string? PaymentTerms { get; init; }
    public string? DeliveryTerms { get; init; }
    public string? DeliveryTime { get; init; }
    public string? FreightTerms { get; init; }
    public string? TaxNotes { get; init; }
    public string Status { get; init; } = QuotationValues.Draft;
    public IReadOnlyList<QuotationItemSnapshot> Items { get; init; } = [];
    public decimal TotalPrice { get; init; }
}

public sealed record QuotationResponse : QuotationSnapshot
{
    public int Id { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime UpdatedDate { get; init; }
}

public sealed record QuotationRevisionSummaryResponse(
    int Id,
    int Revision,
    DateTime CreatedDate);

public sealed record QuotationRevisionResponse(
    int Id,
    int QuotationId,
    int Revision,
    DateTime CreatedDate,
    QuotationSnapshot Snapshot);
