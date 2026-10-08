using ITPQuotation.Api.Documents;
using ITPQuotation.Api.DTOs.Quotations;
using QuestPDF.Infrastructure;

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
                    Quantity = 10m,
                    UnitPrice = 132m,
                    TotalPrice = 1320m
                }
            ],
            TotalPrice = 1320m
        });

        Assert.True(pdf.Length > 100);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }
}
