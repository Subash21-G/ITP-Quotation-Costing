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

    [Fact]
    public void Parse_WegSupplyQuotation_ExtractsCompactTableAndMultilineDetails()
    {
        var result = _parser.Parse(
            "Print data.PDF",
            """
            WEG Ind.India Private Ltd 6006 - WEN - WIND - Hosur - IN
            Supply Quotation No. 6003216261/ WEG     Print Date: 28.09.2026     Page: 1/2
            To: WEG INDUSTRIES (INDIA) PVT. LTD.     Supplier Code: 6200
            Item Material Description Part Number Drawing/ Syst. Code Quant./Unit Delivery Date
            00010 10174478 BEAR PROT RING W/O LAB SAE1010/20 264X12 SWD 10000095239 / SWP 10000095238 1.00/UN 09.06.2027
            SWP 10000095238
            RING BEARING PROTECTION WITHOUT 264mmX120mmX16mm MATERIAL: CARBON STEEL SAE 1010/20; EXTERNAL DIAMETER: 264mm; PART CONDITION: MACHINED
            00020 15628615 BEAR PROT RING W/O LAB SAE1010/20 284X18 SWD 10002028378 1.00/UN 04.08.2027
            RING BEARING PROTECTION WITHOUT 284mmX180mmX16mm MATERIAL: CARBON STEEL SAE 1010/20; EXTERNAL DIAMETER: 284mm; PART CONDITION: MACHINED
            """,
            2);

        Assert.Equal("6003216261/ WEG", result.RfqNumber);
        Assert.Equal("WEG INDUSTRIES (INDIA) PVT. LTD.", result.CustomerName);
        Assert.Equal(new DateTime(2026, 9, 28), result.RfqDate);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("00010", result.Items[0].LineItem);
        Assert.Equal("10174478", result.Items[0].MaterialNo);
        Assert.Equal("SWD 10000095239; SWP 10000095238", result.Items[0].DrawingNo);
        Assert.Equal(1m, result.Items[0].Quantity);
        Assert.Equal("UN", result.Items[0].Unit);
        Assert.Equal(new DateTime(2027, 6, 9), result.Items[0].DeliveryDate);
        Assert.Equal("CARBON STEEL SAE 1010/20", result.Items[0].Grade);
        Assert.Equal("264mmX120mmX16mm", result.Items[0].Dimensions);
        Assert.Contains("RING BEARING PROTECTION", result.Items[0].Description);
        Assert.Equal("00020", result.Items[1].LineItem);
        Assert.Equal("15628615", result.Items[1].MaterialNo);
        Assert.Equal("284mmX180mmX16mm", result.Items[1].Dimensions);
        Assert.Equal(new DateTime(2027, 8, 4), result.Items[1].DeliveryDate);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Parse_WegCableEntryPlate_ExtractsInlineCastIronGrade()
    {
        var result = _parser.Parse(
            "plate.pdf",
            """
            Supply Quotation No. 6003213177/ WEG Print Date: 08.10.2026 Page: 1/1
            To: WEG INDUSTRIES (INDIA) PVT. LTD. Supplier Code: 6200
            00010 19220392 CABLE ENTRY PLATE FC-200 446X861mm
            SWD 10014319518 1.00/UN 14.10.2026
            CABLE ENTRY PLATE CAST IRON FC-200 COMPONENT TYPE: SPECIAL; ENTRY PLATE THICKNESS : 12mm; ENTRY PLATE WIDTH : 446mm; ENTRY PLATE LENGTH : 861mm CABLE ENTRY PLATE CAST IRON FC-200 COMPONENT TYPE: SPECIAL; ENTRY PLATE THICKNESS : 12mm; ENTRY PLATE WIDTH : 446mm; ENTRY PLATE LENGTH : 861mm
            Additional Information:
            """,
            1);

        var item = Assert.Single(result.Items);
        Assert.Equal("CAST IRON FC-200", item.Grade);
        Assert.Equal("446mmX861mmX12mm", item.Dimensions);
        Assert.Equal(1, item.Description!.Split("COMPONENT TYPE").Length - 1);
    }
}
