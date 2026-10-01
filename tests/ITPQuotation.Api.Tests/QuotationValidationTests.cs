using ITPQuotation.Api.DTOs.Quotations;

namespace ITPQuotation.Api.Tests;

public sealed class QuotationValidationTests
{
    [Fact]
    public void Quotation_WithUnsupportedStatus_IsRejected()
    {
        var request = ValidRequest();
        request.Status = "Unknown";

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.Status)));
    }

    [Fact]
    public void Quotation_WithValidityBeforeQuotationDate_IsRejected()
    {
        var request = ValidRequest();
        request.QuotationDate = new DateTime(2026, 9, 28);
        request.ValidUntil = new DateTime(2026, 9, 27);

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.ValidUntil)));
    }

    [Fact]
    public void Quotation_WithDuplicateRfqItem_IsRejected()
    {
        var request = ValidRequest();
        request.Items.Add(new QuotationItemWriteRequest { RfqItemId = 1 });

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, x =>
            x.ErrorMessage!.Contains("duplicated", StringComparison.OrdinalIgnoreCase));
    }

    private static QuotationCreateRequest ValidRequest() =>
        new()
        {
            QuotationNumber = "Q-VALID",
            CustomerId = 1,
            RfqId = 1,
            QuotationDate = new DateTime(2026, 9, 28),
            Status = "Draft",
            Items = [new QuotationItemWriteRequest { RfqItemId = 1 }]
        };
}
