using System.Text.Json.Serialization;

namespace ITPQuotation.Api.Models;

public class Rfq
{
    public int Id { get; set; }

    public string RfqNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public DateTime? RfqDate { get; set; }

    public string? PdfFilePath { get; set; }

    public string Status { get; set; } = "Draft";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ICollection<RfqItem> Items { get; set; } = [];
}
