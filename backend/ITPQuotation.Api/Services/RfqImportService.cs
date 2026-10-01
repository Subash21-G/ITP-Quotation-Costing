using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.RfqImports;
using ITPQuotation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Services;

public sealed class RfqImportService(
    ApplicationDbContext context,
    IRfqPdfTextExtractor textExtractor,
    RfqTextParser parser)
{
    public RfqPdfExtractionResponse Extract(string fileName, Stream pdfStream)
    {
        var extraction = textExtractor.Extract(pdfStream);
        return parser.Parse(fileName, extraction.Text, extraction.PageCount);
    }

    public async Task<RfqImportConfirmationResponse> ConfirmAsync(
        RfqImportConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var rfqNumber = request.RfqNumber.Trim();
        if (string.IsNullOrWhiteSpace(rfqNumber))
            throw new RfqImportValidationException("RFQ number is required.");
        if (request.Items.Count == 0)
            throw new RfqImportValidationException("At least one RFQ item is required.");
        if (!await context.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken))
            throw new RfqImportValidationException("Selected customer does not exist.");
        if (await context.Rfqs.AnyAsync(x => x.RfqNumber == rfqNumber, cancellationToken))
            throw new RfqImportConflictException($"RFQ number '{rfqNumber}' already exists.");

        var rfq = new Rfq
        {
            RfqNumber = rfqNumber,
            CustomerId = request.CustomerId,
            RfqDate = request.RfqDate,
            Status = request.Status
        };

        foreach (var itemRequest in request.Items)
        {
            if (decimal.Round(itemRequest.Quantity, 3) != itemRequest.Quantity)
                throw new RfqImportValidationException(
                    "Item quantity supports up to three decimal places.");

            rfq.Items.Add(new RfqItem
            {
                LineItem = Clean(itemRequest.LineItem),
                MaterialNo = itemRequest.MaterialNo.Trim(),
                Description = Clean(itemRequest.Description),
                DrawingNo = Clean(itemRequest.DrawingNo),
                Grade = Clean(itemRequest.Grade),
                Dimensions = Clean(itemRequest.Dimensions),
                Quantity = itemRequest.Quantity,
                Unit = itemRequest.Unit.Trim(),
                DeliveryDate = itemRequest.DeliveryDate
            });
        }

        context.Rfqs.Add(rfq);
        await context.SaveChangesAsync(cancellationToken);
        return new RfqImportConfirmationResponse(
            rfq.Id,
            rfq.RfqNumber,
            rfq.Items.Count,
            Confirmed: true);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
