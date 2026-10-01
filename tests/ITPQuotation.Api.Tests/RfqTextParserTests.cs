using ITPQuotation.Api.Services;

namespace ITPQuotation.Api.Tests;

public sealed class RfqTextParserTests
{
    private readonly RfqTextParser _parser = new();

    [Fact]
    public void Parse_LabeledRfq_ExtractsHeaderAndMultipleItems()
    {
        var result = _parser.Parse(
            "customer-rfq.pdf",
            """
            RFQ Number: RFQ-2026-77
            Customer Name: Zenith Components
            RFQ Date: 28/09/2026
            Item: 10
            Material Number: MAT-001
            Description: First component
            Drawing: DRW-001
            Quantity: 12.5 Nos
            Delivery Date: 15/10/2026
            Material Grade: EN8
            Size: 50 x 25 x 10 mm
            Item: 20
            Material Number: MAT-002
            Description: Second component
            Quantity: 4 Pcs
            """,
            2);

        Assert.Equal("RFQ-2026-77", result.RfqNumber);
        Assert.Equal("Zenith Components", result.CustomerName);
        Assert.Equal(new DateTime(2026, 9, 28), result.RfqDate);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("10", result.Items[0].LineItem);
        Assert.Equal("MAT-001", result.Items[0].MaterialNo);
        Assert.Equal(12.5m, result.Items[0].Quantity);
        Assert.Equal("Nos", result.Items[0].Unit);
        Assert.Equal("EN8", result.Items[0].Grade);
        Assert.Equal("50 x 25 x 10 mm", result.Items[0].Dimensions);
        Assert.Equal("MAT-002", result.Items[1].MaterialNo);
        Assert.Equal("20", result.Items[1].LineItem);
        Assert.Empty(result.Warnings);
        Assert.True(result.RequiresConfirmation);
    }

    [Fact]
    public void Parse_MissingRequiredSuggestions_ReturnsReviewWarnings()
    {
        var result = _parser.Parse("sparse.pdf", "Description: Unknown part", 1);

        Assert.Contains("RFQ number was not detected.", result.Warnings);
        Assert.Contains("Customer name was not detected.", result.Warnings);
        Assert.Contains("Item 1: material number was not detected.", result.Warnings);
        Assert.Contains("Item 1: quantity was not detected.", result.Warnings);
    }
}
