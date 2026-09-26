namespace ITPQuotation.Api.Models;

public class VendorProcessRate
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public int ProcessId { get; set; }
    public string RateType { get; set; } = MasterDataValues.PerPiece;
    public decimal Rate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }

    public Vendor Vendor { get; set; } = null!;
    public ProcessMaster Process { get; set; } = null!;
}
