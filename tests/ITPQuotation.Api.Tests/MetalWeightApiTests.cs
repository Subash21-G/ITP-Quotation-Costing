using System.Net;
using System.Net.Http.Json;

namespace ITPQuotation.Api.Tests;

public sealed class MetalWeightApiTests(TestApiFactory factory)
    : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CalculatePlate_ReturnsEngineeringResults()
    {
        var response = await _client.PostAsJsonAsync("/api/calculations/metal-weight", new
        {
            shape = "Plate",
            unit = "mm",
            mode = "LengthToWeight",
            densityKgM3 = 7850,
            quantity = 2,
            length = 1000,
            width = 100,
            thickness = 10
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CalculationResponse>();

        Assert.NotNull(result);
        Assert.Equal(7.85m, result.WeightPerPieceKg);
        Assert.Equal(15.70m, result.TotalWeightKg);
    }

    [Fact]
    public async Task CalculateTube_WithImpossibleGeometry_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/calculations/metal-weight", new
        {
            shape = "RoundTube",
            unit = "mm",
            mode = "LengthToWeight",
            densityKgM3 = 7850,
            quantity = 1,
            length = 1000,
            outerDiameter = 50,
            thickness = 25
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Calculate_WithUnsupportedUnit_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/calculations/metal-weight", new
        {
            shape = "RoundBar",
            unit = "yard",
            mode = "LengthToWeight",
            densityKgM3 = 7850,
            quantity = 1,
            length = 1,
            diameter = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record CalculationResponse(decimal WeightPerPieceKg, decimal TotalWeightKg);
}
