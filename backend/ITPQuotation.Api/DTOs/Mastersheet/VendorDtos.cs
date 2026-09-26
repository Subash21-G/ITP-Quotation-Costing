using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.Mastersheet;

public class VendorRequest
{
    [Required, StringLength(200)]
    public string VendorName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ContactPerson { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(320)]
    public string? Email { get; set; }

    [StringLength(1000)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed record VendorResponse(
    int Id,
    string VendorName,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive,
    DateTime CreatedDate,
    DateTime UpdatedDate);
