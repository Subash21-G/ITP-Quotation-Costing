using System.ComponentModel.DataAnnotations;
namespace ITPQuotation.Api.Models;

public class Customer
{
    public int Id { get; set; }

    [Required, StringLength(200)] public string CustomerName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    [EmailAddress] public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

}

