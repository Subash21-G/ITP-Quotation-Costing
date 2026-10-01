using System.Net;
using System.Net.Http.Json;

namespace ITPQuotation.Api.Tests;

public sealed class RawMaterialCostApiTests(TestApiFactory factory)
    : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CalculateRawMaterialCost_ReturnsCompleteBreakdown()
    {
        var response = await _client.PostAsJsonAsync("/api/calculations/raw-material-cost", new
        {
            weightPerPiece = 5.5m,
            quantity = 4m,
            materialRatePerKg = 80m,
            scrapWeight = 2m,
            scrapRecoveryRate = 20m
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CalculationResponse>();

        Assert.NotNull(result);
        Assert.Equal(22m, result.TotalRawWeight);
        Assert.Equal(1760m, result.GrossMaterialCost);
        Assert.Equal(40m, result.ScrapRecovery);
        Assert.Equal(1720m, result.NetMaterialCost);
    }

    [Fact]
    public async Task CalculateRawMaterialCost_WithNegativeNetCost_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/calculations/raw-material-cost", new
        {
            weightPerPiece = 1m,
            quantity = 1m,
            materialRatePerKg = 10m,
            scrapWeight = 2m,
            scrapRecoveryRate = 10m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CalculateRawMaterialCost_WithZeroQuantity_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/calculations/raw-material-cost", new
        {
            weightPerPiece = 5m,
            quantity = 0m,
            materialRatePerKg = 80m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record CalculationResponse(
        decimal TotalRawWeight,
        decimal GrossMaterialCost,
        decimal ScrapRecovery,
        decimal NetMaterialCost);
}
