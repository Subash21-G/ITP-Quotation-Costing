using System.Globalization;
using ITPQuotation.Api.DTOs.Quotations;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ITPQuotation.Api.Documents;

public sealed class QuestPdfQuotationPdfGenerator : IQuotationPdfGenerator
{
    private const string CompanyName = "Indhra Turning Point";
    private static readonly CultureInfo IndiaCulture = CultureInfo.GetCultureInfo("en-IN");

    public byte[] Generate(QuotationSnapshot quotation) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(style => style.FontSize(9).FontFamily("Lato"));
                page.Header().Element(container => ComposeHeader(container, quotation));
                page.Content().PaddingVertical(14).Element(container => ComposeContent(container, quotation));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();

    private static void ComposeHeader(IContainer container, QuotationSnapshot quotation)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(company =>
                {
                    company.Item().Text(CompanyName).Bold().FontSize(18).FontColor(Colors.Blue.Darken3);
                    company.Item().Text("Precision machining and manufacturing").FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(175).AlignRight().Column(details =>
                {
                    details.Item().Text("QUOTATION").Bold().FontSize(15).FontColor(Colors.Blue.Darken3);
                    details.Item().Text($"No. {quotation.QuotationNumber}").SemiBold();
                    details.Item().Text($"Revision {quotation.Revision}");
                });
            });
            column.Item().PaddingTop(10).BorderBottom(1).BorderColor(Colors.Blue.Darken2);
        });
    }

    private static void ComposeContent(IContainer container, QuotationSnapshot quotation)
    {
        container.Column(column =>
        {
            column.Spacing(14);
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(card => ComposeCustomerCard(card, quotation));
                row.ConstantItem(18);
                row.RelativeItem().Element(card => ComposeDetailsCard(card, quotation));
            });

            column.Item().Text("Offered items").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
            column.Item().Element(card => ComposeItemsTable(card, quotation));
            column.Item().AlignRight().Width(230).Element(card => ComposeTotal(card, quotation));
            column.Item().Text("Commercial terms").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
            column.Item().Element(card => ComposeTerms(card, quotation));
            column.Item().PaddingTop(4).Text(
                "We trust this quotation meets your requirements and look forward to your order.")
                .Italic().FontColor(Colors.Grey.Darken2);
        });
    }

    private static void ComposeCustomerCard(IContainer container, QuotationSnapshot quotation) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(3);
            column.Item().Text("CUSTOMER").Bold().FontSize(8).FontColor(Colors.Grey.Darken1);
            column.Item().Text(quotation.CustomerName).SemiBold().FontSize(11);
            column.Item().Text($"RFQ No.: {quotation.RfqNumber}");
        });

    private static void ComposeDetailsCard(IContainer container, QuotationSnapshot quotation) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Spacing(3);
            DetailLine(column, "Date", FormatDate(quotation.QuotationDate));
            DetailLine(column, "Valid until", quotation.ValidUntil is null ? "Not specified" : FormatDate(quotation.ValidUntil.Value));
            DetailLine(column, "Status", quotation.Status);
        });

    private static void DetailLine(ColumnDescriptor column, string label, string value) =>
        column.Item().Row(row =>
        {
            row.RelativeItem().Text(label).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().AlignRight().Text(value).SemiBold();
        });

    private static void ComposeItemsTable(IContainer container, QuotationSnapshot quotation)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.RelativeColumn(2.3f);
                columns.RelativeColumn(3.2f);
                columns.RelativeColumn(1.3f);
                columns.RelativeColumn(1.8f);
                columns.RelativeColumn(2f);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "#");
                HeaderCell(header.Cell(), "Material no.");
                HeaderCell(header.Cell(), "Description / drawing");
                HeaderCell(header.Cell(), "Qty", alignRight: true);
                HeaderCell(header.Cell(), "Unit price", alignRight: true);
                HeaderCell(header.Cell(), "Total", alignRight: true);
            });

            var number = 1;
            foreach (var item in quotation.Items)
            {
                BodyCell(table.Cell(), number++.ToString(CultureInfo.InvariantCulture));
                BodyCell(table.Cell(), item.MaterialNo);
                table.Cell().Element(BodyCellStyle).Column(description =>
                {
                    description.Spacing(2);
                    description.Item().Text(item.Description ?? "-");
                    if (!string.IsNullOrWhiteSpace(item.DrawingNo))
                    {
                        description.Item().Text($"Drawing: {item.DrawingNo}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                });
                BodyCell(table.Cell(), FormatQuantity(item.Quantity), alignRight: true);
                BodyCell(table.Cell(), FormatCurrency(item.UnitPrice), alignRight: true);
                BodyCell(table.Cell(), FormatCurrency(item.TotalPrice), alignRight: true, bold: true);
            }
        });
    }

    private static void ComposeTotal(IContainer container, QuotationSnapshot quotation) =>
        container.Background(Colors.Blue.Lighten5).Padding(10).Row(row =>
        {
            row.RelativeItem().Text("Grand total").Bold().FontSize(11);
            row.RelativeItem().AlignRight().Text(FormatCurrency(quotation.TotalPrice)).Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
        });

    private static void ComposeTerms(IContainer container, QuotationSnapshot quotation) =>
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            Term(column, "Payment terms", quotation.PaymentTerms);
            Term(column, "Delivery terms", quotation.DeliveryTerms);
            Term(column, "Delivery time", quotation.DeliveryTime);
            Term(column, "Freight", quotation.FreightTerms);
            Term(column, "Taxes", quotation.TaxNotes);
        });

    private static void Term(ColumnDescriptor column, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        column.Item().PaddingBottom(3).Row(row =>
        {
            row.ConstantItem(95).Text(label).SemiBold();
            row.RelativeItem().Text(value);
        });
    }

    private static void HeaderCell(IContainer container, string text, bool alignRight = false)
    {
        var cell = container.Background(Colors.Blue.Darken2).Padding(6).AlignMiddle();
        if (alignRight)
        {
            cell = cell.AlignRight();
        }

        cell.Text(text).FontColor(Colors.White).Bold().FontSize(8);
    }

    private static void BodyCell(IContainer container, string text, bool alignRight = false, bool bold = false)
    {
        var cell = container.Element(BodyCellStyle);
        if (alignRight)
        {
            cell = cell.AlignRight();
        }

        var cellText = cell.Text(text);
        if (bold)
        {
            cellText.Bold();
        }
    }

    private static IContainer BodyCellStyle(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignMiddle();

    private static void ComposeFooter(IContainer container) =>
        container.PaddingTop(8).BorderTop(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
        {
            row.RelativeItem().Text("This quotation is generated from the approved quotation revision.").FontSize(8).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().AlignRight().DefaultTextStyle(style =>
                style.FontSize(8).FontColor(Colors.Grey.Darken1)).Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });

    private static string FormatDate(DateTime value) => value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    private static string FormatQuantity(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string FormatCurrency(decimal value) => value.ToString("C2", IndiaCulture);
}
