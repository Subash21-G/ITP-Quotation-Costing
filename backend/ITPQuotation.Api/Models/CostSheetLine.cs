namespace ITPQuotation.Api.Models;

public class CostSheetLine
{
    public int Id { get; set; }
    public int CostSheetId { get; set; }
    public int Sequence { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }

    public CostSheet CostSheet { get; set; } = null!;
}
