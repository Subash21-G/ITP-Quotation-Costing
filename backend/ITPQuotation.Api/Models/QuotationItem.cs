namespace ITPQuotation.Api.Models;

public class QuotationItem
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public int RfqItemId { get; set; }
    public string MaterialNo { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DrawingNo { get; set; }
    public decimal Quantity { get; set; }
    public decimal? MaterialRatePerKg { get; set; }
    public decimal? DensityKgM3 { get; set; }
    public string? RawMaterialShape { get; set; }
    public string? RawMaterialDimensions { get; set; }
    public decimal? WeightPerPieceKg { get; set; }
    public decimal? TotalWeightKg { get; set; }
    public decimal ManufacturingCost { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal OverheadAmount { get; set; }
    public decimal ProfitPercent { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string CostBreakdownJson { get; set; } = "{}";
    public string ProcessRouteJson { get; set; } = "[]";

    public Quotation Quotation { get; set; } = null!;
}
