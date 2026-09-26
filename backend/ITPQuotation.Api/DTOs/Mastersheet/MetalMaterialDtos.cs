using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.Mastersheet;

public class MetalMaterialRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Grade { get; set; }

    [Range(typeof(decimal), "0.000001", "999999999999.999999")]
    public decimal DensityKgM3 { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DefaultRatePerKg { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed record MetalMaterialResponse(
    int Id,
    string Name,
    string? Grade,
    decimal DensityKgM3,
    decimal DefaultRatePerKg,
    bool IsActive,
    DateTime CreatedDate,
    DateTime UpdatedDate);
