using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.Mastersheet;

public class VendorProcessRateRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int VendorId { get; set; }

    [Range(1, int.MaxValue)]
    public int ProcessId { get; set; }

    [Required, RegularExpression("^(PerPiece|PerKg|PerHour|Fixed)$")]
    public string RateType { get; set; } = "PerPiece";

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Rate { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveTo.HasValue && EffectiveTo.Value < EffectiveFrom)
        {
            yield return new ValidationResult(
                "EffectiveTo must be on or after EffectiveFrom.",
                [nameof(EffectiveTo)]);
        }
    }
}

public sealed record VendorProcessRateResponse(
    int Id,
    int VendorId,
    string VendorName,
    int ProcessId,
    string ProcessName,
    string RateType,
    decimal Rate,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string? Notes);
