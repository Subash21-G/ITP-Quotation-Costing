using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.Mastersheet;

public class ProcessMasterRequest
{
    [Required, StringLength(100)]
    public string ProcessName { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required, RegularExpression("^(InHouse|Outsource)$")]
    public string DefaultProcessType { get; set; } = "InHouse";

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DefaultMachineRate { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed record ProcessMasterResponse(
    int Id,
    string ProcessName,
    string? Description,
    string DefaultProcessType,
    decimal DefaultMachineRate,
    bool IsActive,
    DateTime CreatedDate,
    DateTime UpdatedDate);
