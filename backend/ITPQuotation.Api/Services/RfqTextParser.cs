using System.Globalization;
using System.Text.RegularExpressions;
using ITPQuotation.Api.DTOs.RfqImports;

namespace ITPQuotation.Api.Services;

public sealed partial class RfqTextParser
{
    public RfqPdfExtractionResponse Parse(string fileName, string text, int pageCount)
    {
        var lines = text
            .ReplaceLineEndings(((char)10).ToString())
            .Split((char)10, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var rfqNumber = FirstValue(lines, RfqNumberPattern());
        var customer = FirstValue(lines, CustomerPattern());
        var rfqDate = ParseDate(FirstValue(lines, RfqDatePattern()));
        var items = ParseItems(lines);
        var warnings = BuildWarnings(rfqNumber, customer, items);

        return new RfqPdfExtractionResponse(
            fileName,
            pageCount,
            text,
            rfqNumber,
            customer,
            rfqDate,
            items,
            warnings);
    }

    private static IReadOnlyList<RfqImportItemSuggestion> ParseItems(string[] lines)
    {
        var items = new List<RfqImportItemSuggestion>();
        MutableItem? current = null;

        foreach (var line in lines)
        {
            var lineItem = MatchValue(line, LineItemPattern());
            if (lineItem is not null && current?.MaterialNo is not null)
            {
                items.Add(current.ToSuggestion());
                current = new MutableItem { LineItem = lineItem };
                continue;
            }

            var material = MatchValue(line, MaterialPattern());
            if (material is not null)
            {
                if (current?.MaterialNo is not null)
                {
                    items.Add(current.ToSuggestion());
                    current = new MutableItem();
                }
                current ??= new MutableItem();
                current.MaterialNo = material;
                continue;
            }

            var description = MatchValue(line, DescriptionPattern());
            var drawing = MatchValue(line, DrawingPattern());
            var quantity = MatchValue(line, QuantityPattern());
            var deliveryDate = MatchValue(line, DeliveryDatePattern());
            var grade = MatchValue(line, GradePattern());
            var dimensions = MatchValue(line, DimensionsPattern());

            if (lineItem is null && description is null && drawing is null && quantity is null
                && deliveryDate is null && grade is null && dimensions is null)
            {
                continue;
            }

            current ??= new MutableItem();
            current.LineItem ??= lineItem;
            current.Description ??= description;
            current.DrawingNo ??= drawing;
            current.Grade ??= grade;
            current.Dimensions ??= dimensions;
            if (deliveryDate is not null) current.DeliveryDate ??= ParseDate(deliveryDate);
            if (quantity is not null && current.Quantity is null) ParseQuantity(quantity, current);
        }

        if (current?.HasContent == true) items.Add(current.ToSuggestion());
        return items;
    }

    private static void ParseQuantity(string value, MutableItem item)
    {
        var match = QuantityValuePattern().Match(value);
        if (!match.Success) return;

        var number = match.Groups["number"].Value.Replace(',', '.');
        if (decimal.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
            item.Quantity = quantity;

        var unit = match.Groups["unit"].Value;
        if (!string.IsNullOrWhiteSpace(unit)) item.Unit = unit;
    }

    private static IReadOnlyList<string> BuildWarnings(
        string? rfqNumber,
        string? customer,
        IReadOnlyList<RfqImportItemSuggestion> items)
    {
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(rfqNumber)) warnings.Add("RFQ number was not detected.");
        if (string.IsNullOrWhiteSpace(customer)) warnings.Add("Customer name was not detected.");
        if (items.Count == 0) warnings.Add("No RFQ item was detected.");
        for (var index = 0; index < items.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(items[index].MaterialNo))
                warnings.Add($"Item {index + 1}: material number was not detected.");
            if (items[index].Quantity is null)
                warnings.Add($"Item {index + 1}: quantity was not detected.");
        }
        return warnings;
    }

    private static string? FirstValue(IEnumerable<string> lines, Regex pattern) =>
        lines.Select(line => MatchValue(line, pattern)).FirstOrDefault(value => value is not null);

    private static string? MatchValue(string line, Regex pattern)
    {
        var match = pattern.Match(line);
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string[] formats =
        [
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "yyyy-MM-dd", "dd MMM yyyy", "d MMM yyyy", "MMMM d, yyyy"
        ];
        return DateTime.TryParseExact(
            value.Trim(),
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var date)
            ? date
            : null;
    }

    private sealed class MutableItem
    {
        public string? LineItem { get; set; }
        public string? MaterialNo { get; set; }
        public string? Description { get; set; }
        public string? DrawingNo { get; set; }
        public decimal? Quantity { get; set; }
        public string Unit { get; set; } = "Nos";
        public DateTime? DeliveryDate { get; set; }
        public string? Grade { get; set; }
        public string? Dimensions { get; set; }
        public bool HasContent =>
            LineItem is not null || MaterialNo is not null || Description is not null
            || DrawingNo is not null || Quantity is not null || DeliveryDate is not null
            || Grade is not null || Dimensions is not null;

        public RfqImportItemSuggestion ToSuggestion() =>
            new(LineItem, MaterialNo, Description, DrawingNo, Quantity, Unit,
                DeliveryDate, Grade, Dimensions);
    }

    [GeneratedRegex(@"^\s*(?:RFQ|Enquiry|Inquiry)\s*(?:Number|No\.?|#)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex RfqNumberPattern();

    [GeneratedRegex(@"^\s*(?:Customer|Customer Name|Buyer)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex CustomerPattern();

    [GeneratedRegex(@"^\s*(?:RFQ Date|Enquiry Date|Inquiry Date|Date)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex RfqDatePattern();

    [GeneratedRegex(@"^\s*(?:Material Number|Material No\.?|Part Number|Part No\.?|Material)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex MaterialPattern();

    [GeneratedRegex(@"^\s*(?:Line Item|Item|Sl\.?\s*No\.?)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex LineItemPattern();

    [GeneratedRegex(@"^\s*(?:Description|Part Description)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DescriptionPattern();

    [GeneratedRegex(@"^\s*(?:Drawing Number|Drawing No\.?|Drawing)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DrawingPattern();

    [GeneratedRegex(@"^\s*(?:Quantity|Qty\.?)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex QuantityPattern();

    [GeneratedRegex(@"^\s*(?:Delivery Date|Required Date|Due Date)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DeliveryDatePattern();

    [GeneratedRegex(@"^\s*(?:Grade|Material Grade)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex GradePattern();

    [GeneratedRegex(@"^\s*(?:Dimensions|Dimension|Size)\s*[:\-]\s*(?<value>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DimensionsPattern();

    [GeneratedRegex(@"^\s*(?<number>\d+(?:[\.,]\d{1,3})?)\s*(?<unit>[A-Za-z]+)?")]
    private static partial Regex QuantityValuePattern();
}
