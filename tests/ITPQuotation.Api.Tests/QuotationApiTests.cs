using System.Net;
using System.Net.Http.Json;

namespace ITPQuotation.Api.Tests;

public sealed class QuotationApiTests(TestApiFactory factory)
    : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateAndReviseQuotation_PreservesHistoricalCostSnapshot()
    {
        var seed = await CreateCostedRfqItemAsync();
        var request = CreateQuotationRequest(seed);
        var createResponse = await _client.PostAsJsonAsync("/api/quotations", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<QuotationApiResponse>();

        Assert.NotNull(created);
        Assert.Equal(0, created.Revision);
        Assert.Equal(1320m, created.TotalPrice);
        Assert.Equal(132m, Assert.Single(created.Items).UnitPrice);

        var updateCostResponse = await _client.PutAsJsonAsync(
            $"/api/cost-sheets/{seed.CostSheetId}",
            new
            {
                quantity = 10m,
                overheadPercent = 10m,
                profitPercent = 20m,
                lines = new[]
                {
                    new { sequence = 1, category = "Raw Material", amount = 2000m }
                }
            });
        updateCostResponse.EnsureSuccessStatusCode();

        var reviseResponse = await _client.PutAsJsonAsync(
            $"/api/quotations/{created.Id}",
            new
            {
                quotationDate = new DateTime(2026, 9, 29),
                validUntil = new DateTime(2026, 10, 29),
                paymentTerms = "45 days",
                status = "Ready",
                items = new[] { new { rfqItemId = seed.RfqItemId } }
            });
        reviseResponse.EnsureSuccessStatusCode();
        var revised = await reviseResponse.Content.ReadFromJsonAsync<QuotationApiResponse>();

        Assert.NotNull(revised);
        Assert.Equal(1, revised.Revision);
        Assert.Equal(2640m, revised.TotalPrice);

        var revision0 = await _client.GetFromJsonAsync<QuotationRevisionApiResponse>(
            $"/api/quotations/{created.Id}/revisions/0");
        var revision1 = await _client.GetFromJsonAsync<QuotationRevisionApiResponse>(
            $"/api/quotations/{created.Id}/revisions/1");
        Assert.Equal(1320m, revision0!.Snapshot.TotalPrice);
        Assert.Equal(2640m, revision1!.Snapshot.TotalPrice);

        var revisions = await _client.GetFromJsonAsync<List<RevisionSummary>>(
            $"/api/quotations/{created.Id}/revisions");
        Assert.Equal([0, 1], revisions!.Select(x => x.Revision).ToArray());
    }

    [Fact]
    public async Task CreateQuotation_WithDuplicateNumber_ReturnsConflict()
    {
        var seed = await CreateCostedRfqItemAsync();
        var request = CreateQuotationRequest(seed);
        var first = await _client.PostAsJsonAsync("/api/quotations", request);
        var duplicate = await _client.PostAsJsonAsync("/api/quotations", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task QuotedRfq_CannotBeDeleted()
    {
        var seed = await CreateCostedRfqItemAsync();
        var create = await _client.PostAsJsonAsync(
            "/api/quotations",
            CreateQuotationRequest(seed));
        create.EnsureSuccessStatusCode();

        var delete = await _client.DeleteAsync($"/api/rfqs/{seed.RfqId}");

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task CreateQuotation_WhenItemHasNoCostSheet_ReturnsBadRequest()
    {
        var seed = await CreateRfqItemAsync();
        var response = await _client.PostAsJsonAsync(
            "/api/quotations",
            CreateQuotationRequest(new CostedSeed(
                seed.CustomerId,
                seed.RfqId,
                seed.RfqItemId,
                0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<CostedSeed> CreateCostedRfqItemAsync()
    {
        var seed = await CreateRfqItemAsync();
        var response = await _client.PostAsJsonAsync("/api/cost-sheets", new
        {
            rfqItemId = seed.RfqItemId,
            quantity = 10m,
            overheadPercent = 10m,
            profitPercent = 20m,
            lines = new[]
            {
                new { sequence = 1, category = "Raw Material", amount = 1000m }
            }
        });
        response.EnsureSuccessStatusCode();
        var costSheet = await response.Content.ReadFromJsonAsync<IdResponse>();
        return new CostedSeed(
            seed.CustomerId,
            seed.RfqId,
            seed.RfqItemId,
            costSheet!.Id);
    }

    private async Task<RfqSeed> CreateRfqItemAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var customerResponse = await _client.PostAsJsonAsync("/api/customers", new
        {
            customerName = $"Quotation Customer {suffix}"
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<IdResponse>();

        var rfqResponse = await _client.PostAsJsonAsync("/api/rfqs", new
        {
            rfqNumber = $"Q-RFQ-{suffix}",
            customerId = customer!.Id,
            status = "Draft"
        });
        rfqResponse.EnsureSuccessStatusCode();
        var rfq = await rfqResponse.Content.ReadFromJsonAsync<IdResponse>();

        var itemResponse = await _client.PostAsJsonAsync($"/api/rfqs/{rfq!.Id}/items", new
        {
            materialNo = $"Q-MAT-{suffix}",
            description = "Quotation item",
            drawingNo = "Q-DRG",
            quantity = 10m,
            unit = "Nos"
        });
        itemResponse.EnsureSuccessStatusCode();
        var item = await itemResponse.Content.ReadFromJsonAsync<IdResponse>();
        return new RfqSeed(customer.Id, rfq.Id, item!.Id);
    }

    private static object CreateQuotationRequest(CostedSeed seed) => new
    {
        quotationNumber = $"QTN-{Guid.NewGuid():N}",
        customerId = seed.CustomerId,
        rfqId = seed.RfqId,
        quotationDate = new DateTime(2026, 9, 28),
        validUntil = new DateTime(2026, 10, 28),
        paymentTerms = "30 days",
        deliveryTerms = "Ex works",
        deliveryTime = "4 weeks",
        freightTerms = "Extra",
        taxNotes = "Taxes extra",
        status = "Draft",
        items = new[] { new { rfqItemId = seed.RfqItemId } }
    };

    private sealed record IdResponse(int Id);
    private sealed record RfqSeed(int CustomerId, int RfqId, int RfqItemId);
    private sealed record CostedSeed(
        int CustomerId,
        int RfqId,
        int RfqItemId,
        int CostSheetId);
    private sealed record RevisionSummary(int Revision);
    private sealed record QuotationItemApiResponse(decimal UnitPrice);
    private sealed record QuotationSnapshotApiResponse(decimal TotalPrice);
    private sealed record QuotationRevisionApiResponse(
        int Revision,
        QuotationSnapshotApiResponse Snapshot);
    private sealed record QuotationApiResponse(
        int Id,
        int Revision,
        decimal TotalPrice,
        List<QuotationItemApiResponse> Items);
}
