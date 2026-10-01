using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ITPQuotation.Api.DTOs.RfqImports;

namespace ITPQuotation.Api.Tests;

public sealed class RfqImportApiTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Extract_ValidPdf_ReturnsSuggestionsWithoutSaving()
    {
        var before = await _client.GetFromJsonAsync<List<RfqSummary>>("/api/rfqs");
        using var form = CreatePdfForm("customer-rfq.pdf");

        var response = await _client.PostAsync("/api/rfq-imports/extract", form);

        response.EnsureSuccessStatusCode();
        var extraction = await response.Content.ReadFromJsonAsync<RfqPdfExtractionResponse>();
        Assert.NotNull(extraction);
        Assert.Equal("RFQ-PDF-100", extraction.RfqNumber);
        Assert.Equal("MAT-100", Assert.Single(extraction.Items).MaterialNo);
        Assert.True(extraction.RequiresConfirmation);
        var after = await _client.GetFromJsonAsync<List<RfqSummary>>("/api/rfqs");
        Assert.Equal(before!.Count, after!.Count);
    }

    [Fact]
    public async Task Extract_NonPdfFile_ReturnsBadRequest()
    {
        using var form = CreatePdfForm("rfq.txt");

        var response = await _client.PostAsync("/api/rfq-imports/extract", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_ReviewedFields_CreatesRfqAndItem()
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers", new
        {
            customerName = $"PDF Customer {Guid.NewGuid():N}",
            email = "pdf@example.com"
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<IdResponse>();
        var rfqNumber = $"PDF-{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/rfq-imports/confirm", new
        {
            rfqNumber,
            customerId = customer!.Id,
            rfqDate = "2026-09-28",
            status = "Draft",
            items = new[]
            {
                new
                {
                    lineItem = "10",
                    materialNo = "MAT-PDF",
                    description = "Reviewed description",
                    drawingNo = "DWG-PDF",
                    grade = "EN8",
                    dimensions = "Dia 50 x 500 mm",
                    quantity = 25.5m,
                    unit = "Nos",
                    deliveryDate = "2026-10-30"
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var confirmation =
            await response.Content.ReadFromJsonAsync<RfqImportConfirmationResponse>();
        var items = await _client.GetFromJsonAsync<List<RfqItemSummary>>(
            $"/api/rfqs/{confirmation!.RfqId}/items");
        var item = Assert.Single(items!);
        Assert.Equal("MAT-PDF", item.MaterialNo);
        Assert.Equal("EN8", item.Grade);
        Assert.Equal("Dia 50 x 500 mm", item.Dimensions);
    }

    private static MultipartFormDataContent CreatePdfForm(string fileName)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent("%PDF-1.7 test payload"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", fileName);
        return form;
    }

    private sealed record IdResponse(int Id);
    private sealed record RfqSummary(int Id);
    private sealed record RfqItemSummary(
        int Id,
        string MaterialNo,
        string? Grade,
        string? Dimensions);
}
