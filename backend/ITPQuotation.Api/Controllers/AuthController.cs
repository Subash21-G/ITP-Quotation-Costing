using ITPQuotation.Api.DTOs.Auth;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(AuthService service) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request) =>
        (await service.LoginAsync(request)) is { } result ? Ok(result) : Unauthorized();

    [AllowAnonymous, HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var (result, error) = await service.RegisterAsync(request, false);
        return result is null ? BadRequest(error) : Created("api/auth/login", result);
    }

    [Authorize(Roles = AuthService.Admin), HttpPost("users")]
    public async Task<ActionResult<AuthResponse>> CreateUser(RegisterRequest request)
    {
        var (result, error) = await service.RegisterAsync(request, true);
        return result is null ? BadRequest(error) : Created("api/auth/login", result);
    }
}
