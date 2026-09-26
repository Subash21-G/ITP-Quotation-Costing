using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.Mastersheet;

public class MaterialRoutingRequest : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int Sequence { get; set; }

    [Range(1, int.MaxValue)]
    public int ProcessId { get; set; }

    [Required, RegularExpression("^(InHouse|Outsource)$")]
    public string ProcessType { get; set; } = "InHouse";

    public int? VendorId { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal MachineRate { get; set; }

    [Range(typeof(decimal), "0", "999999999999.9999")]
    public decimal SetupTimeHours { get; set; }

    [Range(typeof(decimal), "0", "999999999999.9999")]
    public decimal CycleTimeHours { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal RatePerPiece { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProcessType == "Outsource" && !VendorId.HasValue)
        {
            yield return new ValidationResult(
                "VendorId is required for an outsourced route.",
                [nameof(VendorId)]);
        }

        if (ProcessType == "InHouse" && VendorId.HasValue)
        {
            yield return new ValidationResult(
                "VendorId must be omitted for an in-house route.",
                [nameof(VendorId)]);
        }
    }
}

public sealed record MaterialRoutingResponse(
    int Id,
    int MaterialMasterId,
    int Sequence,
    int ProcessId,
    string ProcessName,
    string ProcessType,
    int? VendorId,
    string? VendorName,
    decimal MachineRate,
    decimal SetupTimeHours,
    decimal CycleTimeHours,
    decimal RatePerPiece,
    string? Notes);
