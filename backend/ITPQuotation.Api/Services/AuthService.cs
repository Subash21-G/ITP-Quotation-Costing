using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ITPQuotation.Api.DTOs.Auth;
using ITPQuotation.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ITPQuotation.Api.Services;

public sealed class AuthService(UserManager<ApplicationUser> users, IConfiguration configuration)
{
    public const string Admin = "Admin";
    public const string Estimator = "Estimator";
    public const string Viewer = "Viewer";
    public static readonly string[] Roles = [Admin, Estimator, Viewer];

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await users.FindByNameAsync(request.Username.Trim());
        return user is not null && await users.CheckPasswordAsync(user, request.Password)
            ? await TokenAsync(user)
            : null;
    }

    public async Task<(AuthResponse? Result, string? Error)> RegisterAsync(RegisterRequest request, bool firstUser)
    {
        var role = firstUser ? Roles.FirstOrDefault(x => x.Equals(request.Role, StringComparison.OrdinalIgnoreCase)) : Viewer;
        if (role is null) return (null, "Role must be Admin, Estimator, or Viewer.");
        var user = new ApplicationUser { UserName = request.Username.Trim(), DisplayName = request.DisplayName?.Trim() };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded) return (null, string.Join(" ", created.Errors.Select(x => x.Description)));
        await users.AddToRoleAsync(user, role);
        return (await TokenAsync(user), null);
    }

    private async Task<AuthResponse> TokenAsync(ApplicationUser user)
    {
        var roles = await users.GetRolesAsync(user);
        var expires = DateTime.UtcNow.AddHours(8);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(ClaimTypes.Name, user.UserName!) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(issuer: configuration["Jwt:Issuer"], audience: configuration["Jwt:Audience"], claims: claims, expires: expires, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, user.UserName!, user.DisplayName ?? user.UserName!, roles.ToList());
    }
}
