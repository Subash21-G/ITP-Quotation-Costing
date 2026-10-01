namespace ITPQuotation.Api.Models;

public class RfqItem
{
    public int Id { get; set; }

    public int RfqId { get; set; }

    public string? LineItem { get; set; }

    public string MaterialNo { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? DrawingNo { get; set; }

    public string? Grade { get; set; }

    public string? Dimensions { get; set; }

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = "Nos";

    public DateTime? DeliveryDate { get; set; }

    public CostSheet? CostSheet { get; set; }
}
