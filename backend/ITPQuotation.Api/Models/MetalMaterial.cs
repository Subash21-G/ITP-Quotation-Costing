namespace ITPQuotation.Api.Models;

public class MetalMaterial
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public decimal DensityKgM3 { get; set; }
    public decimal DefaultRatePerKg { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<MaterialMaster> Materials { get; set; } = [];
}
