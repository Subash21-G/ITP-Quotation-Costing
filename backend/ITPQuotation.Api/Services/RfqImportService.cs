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
        var customerId = await ResolveCustomerIdAsync(request, cancellationToken);
        if (await context.Rfqs.AnyAsync(x => x.RfqNumber == rfqNumber, cancellationToken))
            throw new RfqImportConflictException($"RFQ number '{rfqNumber}' already exists.");

        var rfq = new Rfq
        {
            RfqNumber = rfqNumber,
            CustomerId = customerId,
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

    private async Task<int> ResolveCustomerIdAsync(
        RfqImportConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId > 0)
        {
            if (await context.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken))
                return request.CustomerId;

            throw new RfqImportValidationException("Selected customer does not exist.");
        }

        var customerName = request.CustomerName?.Trim();
        if (string.IsNullOrWhiteSpace(customerName))
            throw new RfqImportValidationException(
                "Select a customer or provide the customer name detected from the RFQ.");

        var existing = await context.Customers
            .FirstOrDefaultAsync(x => x.CustomerName == customerName, cancellationToken);
        if (existing is not null) return existing.Id;

        var customer = new Customer { CustomerName = customerName };
        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }
}
