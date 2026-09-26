using System.Net;
using System.Net.Http.Json;

namespace ITPQuotation.Api.Tests;

public sealed class MasterDataApiTests(TestApiFactory factory)
    : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostMetal_WithZeroDensity_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/metals", new
        {
            name = "Invalid Metal",
            densityKgM3 = 0,
            defaultRatePerKg = 80
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMaterial_WithDuplicateNumber_ReturnsConflict()
    {
        var materialNo = $"DUPLICATE-{Guid.NewGuid():N}";
        var first = await _client.PostAsJsonAsync("/api/materials", new
        {
            materialNo,
            shortDescription = "First",
            isActive = true
        });
        var duplicate = await _client.PostAsJsonAsync("/api/materials", new
        {
            materialNo,
            shortDescription = "Second",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task PostOutsourceRoute_WithoutVendor_ReturnsBadRequest()
    {
        var processResponse = await _client.PostAsJsonAsync("/api/processes", new
        {
            processName = $"Outsource-{Guid.NewGuid():N}",
            defaultProcessType = "Outsource",
            defaultMachineRate = 0,
            isActive = true
        });
        processResponse.EnsureSuccessStatusCode();
        var process = await processResponse.Content.ReadFromJsonAsync<IdResponse>();

        var materialResponse = await _client.PostAsJsonAsync("/api/materials", new
        {
            materialNo = $"ROUTE-{Guid.NewGuid():N}",
            isActive = true
        });
        materialResponse.EnsureSuccessStatusCode();
        var material = await materialResponse.Content.ReadFromJsonAsync<IdResponse>();

        var routeResponse = await _client.PostAsJsonAsync(
            $"/api/materials/{material!.Id}/routing",
            new
            {
                sequence = 1,
                processId = process!.Id,
                processType = "Outsource",
                ratePerPiece = 100
            });

        Assert.Equal(HttpStatusCode.BadRequest, routeResponse.StatusCode);
    }

    private sealed record IdResponse(int Id);
}
