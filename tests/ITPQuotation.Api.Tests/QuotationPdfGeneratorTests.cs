using ITPQuotation.Api.Documents;
using ITPQuotation.Api.DTOs.Quotations;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace ITPQuotation.Api.Tests;

public sealed class QuotationPdfGeneratorTests
{
    public QuotationPdfGeneratorTests() =>
        QuestPDF.Settings.License = LicenseType.Evaluation;

    [Fact]
    public void Generate_CreatesPdfForQuotationSnapshot()
    {
        var generator = new QuestPdfQuotationPdfGenerator();
        var pdf = generator.Generate(new QuotationSnapshot
        {
            QuotationNumber = "QTN-TEST-001",
            CustomerName = "Test Engineering",
            RfqNumber = "RFQ-TEST-001",
            Revision = 0,
            QuotationDate = new DateTime(2026, 10, 6),
            ValidUntil = new DateTime(2026, 11, 5),
            PaymentTerms = "30 days",
            DeliveryTerms = "Ex works",
            DeliveryTime = "4 weeks",
            FreightTerms = "Extra",
            TaxNotes = "Taxes extra",
            Status = "Ready",
            Items =
            [
                new QuotationItemSnapshot
                {
                    MaterialNo = "MAT-TEST-01",
                    Description = "Precision machined component",
                    DrawingNo = "DWG-TEST-01",
                    RawMaterialDimensions = "50 x 30 x 100 mm",
                    Quantity = 10m,
                    UnitPrice = 132m,
                    TotalPrice = 1320m
                }
            ],
            TotalPrice = 1320m
        });

        Assert.True(pdf.Length > 100);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));

        using var document = PdfDocument.Open(new MemoryStream(pdf));
        var text = string.Join(" ", document.GetPages().Select(page => page.Text));
        Assert.Contains("Raw material size", text);
        Assert.Contains("50 x 30 x 100 mm", text);
    }

    [Fact]
    public void Generate_UsesOnlyShortItemDescription()
    {
        var pdf = new QuestPdfQuotationPdfGenerator().Generate(new QuotationSnapshot
        {
            QuotationNumber = "QTN-TEST-002",
            CustomerName = "Test Engineering",
            RfqNumber = "RFQ-TEST-002",
            QuotationDate = new DateTime(2026, 11, 17),
            Items =
            [
                new QuotationItemSnapshot
                {
                    MaterialNo = "MAT-TEST-02",
                    Description = "BEAR PROT RING W/O LAB SAE1010/20 264X12 - " +
                        string.Concat(Enumerable.Repeat("Long technical details. ", 80)),
                    DrawingNo = "SWD 10000095239",
                    Quantity = 1m,
                    UnitPrice = 100m,
                    TotalPrice = 100m
                }
            ],
            TotalPrice = 100m
        });

        using var document = PdfDocument.Open(new MemoryStream(pdf));
        var text = string.Join(" ", document.GetPages().Select(page => page.Text));

        Assert.Equal(1, document.NumberOfPages);
        Assert.Contains("BEAR PROT RING W/O LAB", text);
        Assert.DoesNotContain("Long technical details", text);
        Assert.DoesNotContain("SWD 10000095239", text);
    }

    [Fact]
    public void GenerateAndGenerateDetailedCosting_IncludeEveryQuotationItem()
    {
        var emptyCostBreakdown = new QuotationCostSnapshot(
            [],
            new Dictionary<string, decimal>(),
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m);
        var snapshot = new QuotationSnapshot
        {
            QuotationNumber = "QTN-TEST-MULTI",
            CustomerName = "Test Engineering",
            RfqNumber = "RFQ-TEST-MULTI",
            QuotationDate = new DateTime(2026, 11, 17),
            Items =
            [
                new QuotationItemSnapshot
                {
                    MaterialNo = "MAT-MULTI-01",
                    Quantity = 1m,
                    CostBreakdown = emptyCostBreakdown
                },
                new QuotationItemSnapshot
                {
                    MaterialNo = "MAT-MULTI-02",
                    Quantity = 2m,
                    CostBreakdown = emptyCostBreakdown
                }
            ],
            TotalPrice = 0m
        };
        var generator = new QuestPdfQuotationPdfGenerator();

        var standardPdf = generator.Generate(snapshot);
        var detailedPdf = generator.GenerateDetailedCosting(snapshot);

        using var standardDocument = PdfDocument.Open(new MemoryStream(standardPdf));
        using var detailedDocument = PdfDocument.Open(new MemoryStream(detailedPdf));
        var standardText = string.Join(" ", standardDocument.GetPages().Select(page => page.Text));
        var detailedText = string.Join(" ", detailedDocument.GetPages().Select(page => page.Text));

        Assert.Contains("MAT-MULTI-01", standardText);
        Assert.Contains("MAT-MULTI-02", standardText);
        Assert.Contains("MAT-MULTI-01", detailedText);
        Assert.Contains("MAT-MULTI-02", detailedText);
    }

    [Fact]
    public void GenerateDetailedCosting_IncludesCostColumnsAndItemizedProcessCosts()
    {
        var pdf = new QuestPdfQuotationPdfGenerator().GenerateDetailedCosting(new QuotationSnapshot
        {
            QuotationNumber = "QTN-TEST-003",
            CustomerName = "Test Engineering",
            RfqNumber = "RFQ-TEST-003",
            QuotationDate = new DateTime(2026, 11, 17),
            PaymentTerms = "30 days",
            DeliveryTerms = "Ex works",
            Items =
            [
                new QuotationItemSnapshot
                {
                    MaterialNo = "MAT-TEST-03",
                    Description = "Coupling - detailed description",
                    RawMaterialDimensions = "Dia250x230",
                    Quantity = 1m,
                    TotalWeightKg = 88.6m,
                    MaterialRatePerKg = 110m,
                    CostBreakdown = new QuotationCostSnapshot(
                        [
                            new QuotationCostLineSnapshot(1, "Raw Material", "Raw material", 1000m),
                            new QuotationCostLineSnapshot(2, "Cutting", "Laser cutting", 100m),
                            new QuotationCostLineSnapshot(3, "Turning", "Turning - Mohan Engg (Outsource; POTracker)", 300m),
                            new QuotationCostLineSnapshot(4, "Welding", "Welding - AmmanWeld (Outsource; POTracker)", 200m),
                            new QuotationCostLineSnapshot(5, "Transport", "Transport (5%)", 71.5m)
                        ],
                        new Dictionary<string, decimal>
                        {
                            ["Raw Material"] = 1000m,
                            ["Cutting"] = 100m,
                            ["Turning"] = 300m,
                            ["Welding"] = 200m,
                            ["Transport"] = 71.5m
                        },
                        1600m,
                        0m,
                        0m,
                        1600m,
                        10m,
                        50m,
                        1721.5m,
                        1721.5m),
                    UnitPrice = 1721.5m,
                    TotalPrice = 1721.5m
                }
            ],
            TotalPrice = 1721.5m
        });

        using var document = PdfDocument.Open(new MemoryStream(pdf));
        var page = Assert.Single(document.GetPages());
        var text = page.Text;

        Assert.True(page.Width > page.Height);
        Assert.Contains("DETAILED COST QUOTATION", text);
        Assert.Contains("Basic Cost", text);
        Assert.Contains("Operations", text);
        Assert.Contains("Process Cost", text);
        Assert.Contains("Process Total", text);
        Assert.Contains("Raw Material Size", text);
        Assert.Contains("Turning", text);
        Assert.Contains("Welding", text);
        Assert.Contains("300.00", text);
        Assert.Contains("200.00", text);
        Assert.Contains("500.00", text);
        Assert.DoesNotContain("Turning, Welding", text);
        Assert.True(text.IndexOf("Turning", StringComparison.Ordinal) <
            text.IndexOf("300.00", StringComparison.Ordinal));
        Assert.True(text.IndexOf("300.00", StringComparison.Ordinal) <
            text.IndexOf("Welding", StringComparison.Ordinal));
        Assert.True(text.IndexOf("Welding", StringComparison.Ordinal) <
            text.IndexOf("200.00", StringComparison.Ordinal));
        Assert.DoesNotContain("Mohan Engg", text);
        Assert.DoesNotContain("AmmanWeld", text);
        Assert.DoesNotContain("Outsource", text);
        Assert.DoesNotContain("POTracker", text);
        Assert.Contains("Dia250x230", text);
    }
}
