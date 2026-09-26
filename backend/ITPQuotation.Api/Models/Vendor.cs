namespace ITPQuotation.Api.Models;

public class Vendor
{
    public int Id { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<VendorProcessRate> ProcessRates { get; set; } = [];
    public ICollection<MaterialRouting> MaterialRoutings { get; set; } = [];
}
