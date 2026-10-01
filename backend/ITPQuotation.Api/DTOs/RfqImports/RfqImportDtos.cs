using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.RfqImports;

public sealed record RfqPdfExtractionResponse(
    string FileName,
    int PageCount,
    string ExtractedText,
    string? RfqNumber,
    string? CustomerName,
    DateTime? RfqDate,
    IReadOnlyList<RfqImportItemSuggestion> Items,
    IReadOnlyList<string> Warnings,
    bool RequiresConfirmation = true);

public sealed record RfqImportItemSuggestion(
    string? LineItem,
    string? MaterialNo,
    string? Description,
    string? DrawingNo,
    decimal? Quantity,
    string Unit,
    DateTime? DeliveryDate,
    string? Grade,
    string? Dimensions);

public sealed class RfqImportConfirmationRequest
{
    [Required, StringLength(100)]
    public string RfqNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    public DateTime? RfqDate { get; set; }

    [Required, RegularExpression("^(Draft|Submitted|Closed|Cancelled)$")]
    public string Status { get; set; } = "Draft";

    [Required, MinLength(1)]
    public List<RfqImportItemConfirmation> Items { get; set; } = [];
}

public sealed class RfqImportItemConfirmation
{
    [StringLength(100)]
    public string? LineItem { get; set; }

    [Required, StringLength(100)]
    public string MaterialNo { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? DrawingNo { get; set; }

    [StringLength(100)]
    public string? Grade { get; set; }

    [StringLength(500)]
    public string? Dimensions { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal Quantity { get; set; }

    [Required, StringLength(30)]
    public string Unit { get; set; } = "Nos";

    public DateTime? DeliveryDate { get; set; }
}

public sealed record RfqImportConfirmationResponse(
    int RfqId,
    string RfqNumber,
    int ItemCount,
    bool Confirmed);
