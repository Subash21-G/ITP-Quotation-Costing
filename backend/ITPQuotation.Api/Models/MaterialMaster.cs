namespace ITPQuotation.Api.Models;

public class MaterialMaster
{
    public int Id { get; set; }
    public string MaterialNo { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Grade { get; set; }
    public string? DrawingCode { get; set; }
    public string? DrawingVersion { get; set; }
    public string? FinishSize { get; set; }
    public int? MetalMaterialId { get; set; }
    public string? RawMaterialShape { get; set; }
    public string? RawMaterialSize { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public MetalMaterial? MetalMaterial { get; set; }
    public ICollection<MaterialRouting> Routings { get; set; } = [];
}
