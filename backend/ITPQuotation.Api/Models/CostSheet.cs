namespace ITPQuotation.Api.Models;

public class CostSheet
{
    public int Id { get; set; }
    public int RfqItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal OverheadPercent { get; set; }
    public decimal ProfitPercent { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public RfqItem RfqItem { get; set; } = null!;
    public ICollection<CostSheetLine> Lines { get; set; } = [];
}
