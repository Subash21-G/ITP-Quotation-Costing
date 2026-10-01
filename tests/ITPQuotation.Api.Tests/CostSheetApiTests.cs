using System.Net;
using System.Net.Http.Json;

namespace ITPQuotation.Api.Tests;

public sealed class CostSheetApiTests(TestApiFactory factory)
    : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateAndGetCostSheet_ReturnsCompleteBreakdown()
    {
        var itemId = await CreateRfqItemAsync();
        var createResponse = await _client.PostAsJsonAsync(
            "/api/cost-sheets",
            CreateRequest(itemId));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CostSheetApiResponse>();
        Assert.NotNull(created);
        Assert.Equal(itemId, created.RfqItemId);
        Assert.Equal(1500m, created.ManufacturingCost);
        Assert.Equal(150m, created.OverheadAmount);
        Assert.Equal(1980m, created.SellingPrice);
        Assert.Equal(198m, created.UnitSellingPrice);
        Assert.Equal(18, created.CategoryTotals.Count);
        Assert.Equal(0m, created.CategoryTotals["Transport"]);

        var getResponse = await _client.GetAsync($"/api/cost-sheets/{created.Id}");
        getResponse.EnsureSuccessStatusCode();
        var loaded = await getResponse.Content.ReadFromJsonAsync<CostSheetApiResponse>();
        Assert.Equal(created.Id, loaded!.Id);
        Assert.Equal(2, loaded.Lines.Count);
    }

    [Fact]
    public async Task UpdateAndDeleteCostSheet_UpdatesAggregateAndRemovesIt()
    {
        var itemId = await CreateRfqItemAsync();
        var createResponse = await _client.PostAsJsonAsync(
            "/api/cost-sheets",
            CreateRequest(itemId));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CostSheetApiResponse>();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/cost-sheets/{created!.Id}",
            new
            {
                quantity = 4m,
                overheadPercent = 5m,
                profitPercent = 10m,
                lines = new[]
                {
                    new
                    {
                        sequence = 1,
                        category = "Packing",
                        description = "Export packing",
                        amount = 400m
                    }
                }
            });

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<CostSheetApiResponse>();
        Assert.NotNull(updated);
        Assert.Single(updated.Lines);
        Assert.Equal(462m, updated.SellingPrice);
        Assert.Equal(115.5m, updated.UnitSellingPrice);

        var deleteResponse = await _client.DeleteAsync($"/api/cost-sheets/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.GetAsync($"/api/cost-sheets/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task CreateSecondCostSheetForItem_ReturnsConflict()
    {
        var itemId = await CreateRfqItemAsync();
        var request = CreateRequest(itemId);
        var first = await _client.PostAsJsonAsync("/api/cost-sheets", request);
        var duplicate = await _client.PostAsJsonAsync("/api/cost-sheets", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task CreateCostSheetWithInvalidCategory_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/cost-sheets",
            new
            {
                rfqItemId = 1,
                quantity = 10m,
                overheadPercent = 10m,
                profitPercent = 20m,
                lines = new[]
                {
                    new { sequence = 1, category = "Invalid", amount = 100m }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> CreateRfqItemAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var customerResponse = await _client.PostAsJsonAsync("/api/customers", new
        {
            customerName = $"Cost Sheet Customer {suffix}"
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<IdResponse>();

        var rfqResponse = await _client.PostAsJsonAsync("/api/rfqs", new
        {
            rfqNumber = $"COST-{suffix}",
            customerId = customer!.Id,
            status = "Draft"
        });
        rfqResponse.EnsureSuccessStatusCode();
        var rfq = await rfqResponse.Content.ReadFromJsonAsync<IdResponse>();

        var itemResponse = await _client.PostAsJsonAsync($"/api/rfqs/{rfq!.Id}/items", new
        {
            materialNo = $"MATERIAL-{suffix}",
            quantity = 10m,
            unit = "Nos"
        });
        itemResponse.EnsureSuccessStatusCode();
        var item = await itemResponse.Content.ReadFromJsonAsync<IdResponse>();
        return item!.Id;
    }

    private static object CreateRequest(int rfqItemId) => new
    {
        rfqItemId,
        quantity = 10m,
        overheadPercent = 10m,
        profitPercent = 20m,
        lines = new[]
        {
            new
            {
                sequence = 1,
                category = "Raw Material",
                description = "Steel",
                amount = 1000m
            },
            new
            {
                sequence = 2,
                category = "CNC",
                description = "Machining",
                amount = 500m
            }
        }
    };

    private sealed record IdResponse(int Id);

    private sealed record CostSheetApiResponse(
        int Id,
        int RfqItemId,
        decimal ManufacturingCost,
        decimal OverheadAmount,
        decimal SellingPrice,
        decimal UnitSellingPrice,
        List<CostSheetLineApiResponse> Lines,
        Dictionary<string, decimal> CategoryTotals);

    private sealed record CostSheetLineApiResponse(
        int Id,
        int Sequence,
        string Category,
        string? Description,
        decimal Amount);
}
