namespace ITPQuotation.Api.Models;

public class QuotationRevision
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public int Revision { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Quotation Quotation { get; set; } = null!;
}
