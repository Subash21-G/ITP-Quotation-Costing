using Microsoft.AspNetCore.Identity;

namespace ITPQuotation.Api.Models;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
