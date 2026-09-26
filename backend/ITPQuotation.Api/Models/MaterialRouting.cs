namespace ITPQuotation.Api.Models;

public class MaterialRouting
{
    public int Id { get; set; }
    public int MaterialMasterId { get; set; }
    public int Sequence { get; set; }
    public int ProcessId { get; set; }
    public string ProcessType { get; set; } = MasterDataValues.InHouse;
    public int? VendorId { get; set; }
    public decimal MachineRate { get; set; }
    public decimal SetupTimeHours { get; set; }
    public decimal CycleTimeHours { get; set; }
    public decimal RatePerPiece { get; set; }
    public string? Notes { get; set; }

    public MaterialMaster MaterialMaster { get; set; } = null!;
    public ProcessMaster Process { get; set; } = null!;
    public Vendor? Vendor { get; set; }
}
