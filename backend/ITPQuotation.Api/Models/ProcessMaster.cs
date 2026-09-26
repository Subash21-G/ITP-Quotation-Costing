namespace ITPQuotation.Api.Models;

public class ProcessMaster
{
    public int Id { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultProcessType { get; set; } = MasterDataValues.InHouse;
    public decimal DefaultMachineRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<VendorProcessRate> VendorProcessRates { get; set; } = [];
    public ICollection<MaterialRouting> MaterialRoutings { get; set; } = [];
}
