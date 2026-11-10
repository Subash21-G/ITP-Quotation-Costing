using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ITPQuotation.Api.DTOs.Auth;
using ITPQuotation.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ITPQuotation.Api.Tests;

public sealed class AuthenticationApiTests
{
    [Fact]
    public async Task AnonymousBusinessRequestsRequireAuthentication()
    {
        await using var factory = new TestApiFactory { UseTestAuthentication = false };
        using var client = factory.CreateClient();
        foreach (var path in new[] { "/api/customers", "/api/rfqs", "/api/materials", "/api/quotations", "/api/quotations/1/pdf" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task PublicRegistrationCannotGrantAdminAndViewerCannotWrite()
    {
        await using var factory = new TestApiFactory { UseTestAuthentication = false };
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = "viewer", Password = "TestOnly!123", Role = "Admin"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(["Viewer"], session.Roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/customers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync("/api/rfqs/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/customers", new { name = "Blocked" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/auth/users", new RegisterRequest
        {
            Username = "admin", Password = "TestOnly!123", Role = "Admin"
        })).StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Estimator")]
    public async Task EditorsCanWriteAndLoginWithIssuedCredentials(string role)
    {
        await using var factory = new TestApiFactory { UseTestAuthentication = false };
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var (_, error) = await scope.ServiceProvider.GetRequiredService<AuthService>().RegisterAsync(
                new RegisterRequest { Username = "editor", Password = "TestOnly!123", Role = role }, true);
            Assert.Null(error);
        }
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Username = "editor", Password = "TestOnly!123" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        // A missing record reaches the action, proving authorization allowed the write.
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/rfqs/987654")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "editor", Password = "WrongPassword!123" })).StatusCode);
    }
}
