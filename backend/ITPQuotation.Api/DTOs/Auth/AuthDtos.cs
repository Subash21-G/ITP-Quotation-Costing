using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.Auth;

public record LoginRequest
{
    [Required, StringLength(100)] public string Username { get; init; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; init; } = string.Empty;
}

public sealed record RegisterRequest : LoginRequest
{
    [StringLength(100)] public string? DisplayName { get; init; }
    [Required] public string Role { get; init; } = "Viewer";
}

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, string Username, string DisplayName, IReadOnlyList<string> Roles);
