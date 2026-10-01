using System.Net;
using System.Net.Http.Json;

namespace ITPQuotation.Api.Tests;

public sealed class ProcessCostApiTests(TestApiFactory factory)
    : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CalculateInHouseProcessCost_ReturnsCompleteBreakdown()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/calculations/process-cost/in-house",
            new
            {
                setupTime = 1m,
                cycleTime = 0.25m,
                quantity = 8m,
                machineRatePerHour = 1000m,
                labourCost = 200m,
                toolCost = 300m
            });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<InHouseResponse>();

        Assert.NotNull(result);
        Assert.Equal(3m, result.TotalProcessTime);
        Assert.Equal(3000m, result.MachineCost);
        Assert.Equal(3500m, result.TotalProcessCost);
    }

    [Fact]
    public async Task CalculateOutsourceProcessCost_ReturnsRateBreakdown()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/calculations/process-cost/outsource",
            new
            {
                rateType = "PerKg",
                rate = 75m,
                totalWeightKg = 20m
            });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OutsourceResponse>();

        Assert.NotNull(result);
        Assert.Equal("PerKg", result.RateType);
        Assert.Equal(20m, result.ChargeableAmount);
        Assert.Equal("kg", result.ChargeableUnit);
        Assert.Equal(1500m, result.TotalProcessCost);
    }

    [Fact]
    public async Task CalculateOutsourceProcessCost_WithMissingBasis_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/calculations/process-cost/outsource",
            new
            {
                rateType = "PerHour",
                rate = 500m
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record InHouseResponse(
        decimal TotalProcessTime,
        decimal MachineCost,
        decimal TotalProcessCost);

    private sealed record OutsourceResponse(
        string RateType,
        decimal ChargeableAmount,
        string ChargeableUnit,
        decimal TotalProcessCost);
}
