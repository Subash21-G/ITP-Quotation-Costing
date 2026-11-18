using System.Globalization;
using ITPQuotation.Api.DTOs.Quotations;
using ITPQuotation.Api.Models;
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

    public byte[] GenerateDetailedCosting(QuotationSnapshot quotation) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A3.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(8).FontFamily("Lato"));
                page.Header().Element(header => ComposeDetailedHeader(header, quotation));
                page.Content().PaddingVertical(12).Element(content => ComposeDetailedContent(content, quotation));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();

    private static void ComposeDetailedHeader(IContainer container, QuotationSnapshot quotation)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(company =>
                {
                    company.Item().Text(CompanyName).Bold().FontSize(18).FontColor(Colors.Blue.Darken3);
                    company.Item().Text("DETAILED COST QUOTATION").Bold().FontSize(12);
                    company.Item().Text($"Customer: {quotation.CustomerName}  |  RFQ: {quotation.RfqNumber}")
                        .FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(250).AlignRight().Column(details =>
                {
                    details.Item().Text($"No. {quotation.QuotationNumber}").SemiBold();
                    details.Item().Text($"Revision {quotation.Revision}");
                    details.Item().Text($"Date: {FormatDate(quotation.QuotationDate)}");
                });
            });
            column.Item().PaddingTop(8).BorderBottom(1).BorderColor(Colors.Blue.Darken2);
        });
    }

    private static void ComposeDetailedContent(IContainer container, QuotationSnapshot quotation)
    {
        container.Column(column =>
        {
            column.Spacing(8);
            column.Item().Element(table => ComposeDetailedItemsTable(table, quotation));
            column.Item().AlignRight().Width(270).Element(total =>
                total.Background(Colors.Blue.Lighten5).Padding(10).Row(row =>
                {
                    row.RelativeItem().Text("Quotation total").Bold().FontSize(11);
                    row.RelativeItem().AlignRight().Text(FormatCurrency(quotation.TotalPrice))
                        .Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                }));
            column.Item().Text(
                $"Payment: {quotation.PaymentTerms ?? "-"}   |   Delivery: {quotation.DeliveryTerms ?? "-"}   |   " +
                $"Time: {quotation.DeliveryTime ?? "-"}   |   Freight: {quotation.FreightTerms ?? "-"}")
                .FontSize(8);
            if (!string.IsNullOrWhiteSpace(quotation.TaxNotes))
            {
                column.Item().Text($"Taxes: {quotation.TaxNotes}").FontSize(8);
            }
        });
    }

    private static void ComposeDetailedItemsTable(IContainer container, QuotationSnapshot quotation)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(24);
                columns.RelativeColumn(1.3f);
                columns.RelativeColumn(1.6f);
                columns.RelativeColumn(1.5f);
                columns.ConstantColumn(35);
                columns.ConstantColumn(45);
                columns.RelativeColumn(0.9f);
                columns.RelativeColumn(1.05f);
                columns.RelativeColumn(0.95f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(2.5f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(0.95f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(0.95f);
                columns.RelativeColumn(1.0f);
            });

            table.Header(header =>
            {
                DetailedHeaderCell(header.Cell(), "#");
                DetailedHeaderCell(header.Cell(), "Material No.");
                DetailedHeaderCell(header.Cell(), "Short Text");
                DetailedHeaderCell(header.Cell(), "Raw Material Size");
                DetailedHeaderCell(header.Cell(), "Qty", alignRight: true);
                DetailedHeaderCell(header.Cell(), "WT kg", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Rs/kg", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Raw Material", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Cutting", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Basic Cost", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Operations");
                DetailedHeaderCell(header.Cell(), "Process Cost", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Process Total", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Profit", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Subtotal", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Transport", alignRight: true);
                DetailedHeaderCell(header.Cell(), "Total", alignRight: true);
            });

            var number = 1;
            foreach (var item in quotation.Items)
            {
                var rawMaterial = CategoryTotal(item, CostSheetValues.RawMaterial);
                var cutting = CategoryTotal(item, CostSheetValues.Cutting);
                var transport = CategoryTotal(item, CostSheetValues.Transport);
                DetailedBodyCell(table.Cell(), number++.ToString(CultureInfo.InvariantCulture));
                DetailedBodyCell(table.Cell(), item.MaterialNo);
                DetailedBodyCell(table.Cell(), ShortDescription(item.Description));
                DetailedBodyCell(table.Cell(), item.RawMaterialDimensions ?? "-");
                DetailedBodyCell(table.Cell(), FormatQuantity(item.Quantity), alignRight: true);
                DetailedBodyCell(table.Cell(), FormatQuantity(
                    item.TotalWeightKg ?? (item.WeightPerPieceKg.HasValue
                        ? item.WeightPerPieceKg.Value * item.Quantity
                        : 0m)), alignRight: true);
                DetailedBodyCell(table.Cell(), item.MaterialRatePerKg.HasValue
                    ? FormatCurrency(item.MaterialRatePerKg.Value)
                    : "-", alignRight: true);
                DetailedBodyCell(table.Cell(), FormatCurrency(rawMaterial), alignRight: true);
                DetailedBodyCell(table.Cell(), FormatCurrency(cutting), alignRight: true);
                DetailedBodyCell(table.Cell(), FormatCurrency(rawMaterial + cutting), alignRight: true);
                table.Cell().ColumnSpan(2)
                    .Element(cell => ComposeDetailedProcessCosts(cell, item));
                DetailedBodyCell(table.Cell(), FormatCurrency(
                    ProcessCostLines(item).Sum(line => line.Amount)), alignRight: true);
                DetailedBodyCell(table.Cell(), FormatCurrency(item.CostBreakdown?.ProfitAmount ?? 0m), alignRight: true);
                DetailedBodyCell(table.Cell(), FormatCurrency(
                    (item.CostBreakdown?.CostAfterOverhead ?? item.CostBreakdown?.ManufacturingCost ?? 0m) +
                    (item.CostBreakdown?.ProfitAmount ?? 0m)), alignRight: true);
                DetailedBodyCell(table.Cell(), FormatCurrency(transport), alignRight: true);
                DetailedBodyCell(table.Cell(), FormatCurrency(item.TotalPrice), alignRight: true, bold: true);
            }
        });
    }

    private static decimal CategoryTotal(QuotationItemSnapshot item, string category)
    {
        if (item.CostBreakdown is null)
            return 0m;

        return item.CostBreakdown.CategoryTotals.TryGetValue(category, out var value)
            ? value
            : 0m;
    }

    private static IEnumerable<QuotationCostLineSnapshot> ProcessCostLines(
        QuotationItemSnapshot item) =>
        item.CostBreakdown?.Lines.Where(line =>
            !line.Category.Equals(CostSheetValues.RawMaterial, StringComparison.OrdinalIgnoreCase) &&
            !line.Category.Equals(CostSheetValues.Cutting, StringComparison.OrdinalIgnoreCase) &&
            !line.Category.Equals(CostSheetValues.Transport, StringComparison.OrdinalIgnoreCase))
        ?? [];

    private static void ComposeDetailedProcessCosts(
        IContainer container,
        QuotationItemSnapshot item)
    {
        var processLines = ProcessCostLines(item)
            .Where(line => line.Amount != 0m)
            .Select(line => (Name: ProcessDisplayName(item, line), Cost: FormatCurrency(line.Amount)))
            .ToArray();
        var rows = processLines.Length > 0
            ? processLines
            : item.ProcessRoute
                .Select(route => (Name: route.ProcessName, Cost: "-"))
                .ToArray();
        if (rows.Length == 0)
            rows = [("-", "-")];

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.5f);
                columns.RelativeColumn(1);
            });

            foreach (var row in rows)
            {
                DetailedProcessCell(table.Cell(), row.Name, alignRight: false);
                DetailedProcessCell(table.Cell(), row.Cost, alignRight: true);
            }
        });
    }

    private static void DetailedProcessCell(
        IContainer container,
        string text,
        bool alignRight)
    {
        var cell = container.Border(0.5f)
            .BorderColor(Colors.Grey.Lighten2).Padding(4).AlignMiddle();
        if (alignRight)
            cell = cell.AlignRight();
        cell.Text(text).FontSize(7);
    }

    private static string ProcessDisplayName(
        QuotationItemSnapshot item,
        QuotationCostLineSnapshot line)
    {
        var description = line.Description?.Trim();
        if (string.IsNullOrWhiteSpace(description))
            return line.Category;

        var route = item.ProcessRoute.FirstOrDefault(entry =>
            description.StartsWith(entry.ProcessName, StringComparison.OrdinalIgnoreCase));
        if (route is not null)
            return route.ProcessName;

        var separator = description.IndexOf(" - ", StringComparison.Ordinal);
        if (separator > 0)
            return description[..separator].Trim();

        var metadata = description.IndexOf(" (", StringComparison.Ordinal);
        return metadata > 0 ? description[..metadata].Trim() : description;
    }

    private static void DetailedHeaderCell(IContainer container, string text, bool alignRight = false)
    {
        var cell = container.Background(Colors.Blue.Darken2)
            .Border(0.5f).BorderColor(Colors.White).Padding(4).AlignMiddle();
        if (alignRight) cell = cell.AlignRight();
        cell.Text(text).FontColor(Colors.White).Bold().FontSize(7);
    }

    private static void DetailedBodyCell(
        IContainer container,
        string text,
        bool alignRight = false,
        bool bold = false)
    {
        var cell = container.Border(0.5f)
            .BorderColor(Colors.Grey.Lighten2).Padding(4).AlignMiddle();
        if (alignRight) cell = cell.AlignRight();
        var value = cell.Text(text).FontSize(7);
        if (bold) value.Bold();
    }

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
                columns.RelativeColumn(1.8f);
                columns.RelativeColumn(2.0f);
                columns.RelativeColumn(3.4f);
                columns.RelativeColumn(1.0f);
                columns.RelativeColumn(1.6f);
                columns.RelativeColumn(1.8f);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "#");
                HeaderCell(header.Cell(), "Material no.");
                HeaderCell(header.Cell(), "Raw material size");
                HeaderCell(header.Cell(), "Short description");
                HeaderCell(header.Cell(), "Qty", alignRight: true);
                HeaderCell(header.Cell(), "Unit price", alignRight: true);
                HeaderCell(header.Cell(), "Total", alignRight: true);
            });

            var number = 1;
            foreach (var item in quotation.Items)
            {
                BodyCell(table.Cell(), number++.ToString(CultureInfo.InvariantCulture));
                BodyCell(table.Cell(), item.MaterialNo);
                BodyCell(table.Cell(), item.RawMaterialDimensions ?? "-");
                BodyCell(table.Cell(), ShortDescription(item.Description));
                BodyCell(table.Cell(), FormatQuantity(item.Quantity), alignRight: true);
                BodyCell(table.Cell(), FormatCurrency(item.UnitPrice), alignRight: true);
                BodyCell(table.Cell(), FormatCurrency(item.TotalPrice), alignRight: true, bold: true);
            }
        });
    }

    private static string ShortDescription(string? description)
    {
        var concise = description?.Split(" - ", 2, StringSplitOptions.None)[0].Trim();
        return string.IsNullOrWhiteSpace(concise) ? "-" : concise;
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
