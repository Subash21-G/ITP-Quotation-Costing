using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs;

public class RfqRequest
{
    [Required, StringLength(100)]
    public string RfqNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    public DateTime? RfqDate { get; set; }

    [Required, RegularExpression("^(Draft|Submitted|Closed|Cancelled)$")]
    public string Status { get; set; } = "Draft";
}

public class RfqItemRequest
{
    [StringLength(100)]
    public string? LineItem { get; set; }

    [Required, StringLength(100)]
    public string MaterialNo { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? DrawingNo { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal Quantity { get; set; }

    [Required, StringLength(30)]
    public string Unit { get; set; } = "Nos";

    public DateTime? DeliveryDate { get; set; }
}
