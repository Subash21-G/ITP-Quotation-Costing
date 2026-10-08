namespace ITPQuotation.Api.Models;

public class Quotation
{
    public int Id { get; set; }
    public int? PoTrackerPurchaseOrderId { get; set; }
    public string? PoTrackerPoNumber { get; set; }
    public int? PoTrackerExportedRevision { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int RfqId { get; set; }
    public int Revision { get; set; }
    public DateTime QuotationDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public string? DeliveryTime { get; set; }
    public string? FreightTerms { get; set; }
    public string? TaxNotes { get; set; }
    public string Status { get; set; } = QuotationValues.Draft;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public Rfq Rfq { get; set; } = null!;
    public ICollection<QuotationItem> Items { get; set; } = [];
    public ICollection<QuotationRevision> Revisions { get; set; } = [];
}
