using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace ITPQuotation.Api.Common;

public sealed class MasterDataExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            MasterDataConflictException =>
                (StatusCodes.Status409Conflict, "Master data conflict"),
            MasterDataValidationException =>
                (StatusCodes.Status400BadRequest, "Master data validation failed"),
            CostSheetConflictException =>
                (StatusCodes.Status409Conflict, "Cost sheet conflict"),
            CostSheetValidationException =>
                (StatusCodes.Status400BadRequest, "Cost sheet validation failed"),
            QuotationConflictException =>
                (StatusCodes.Status409Conflict, "Quotation conflict"),
            QuotationValidationException =>
                (StatusCodes.Status400BadRequest, "Quotation validation failed"),
            RfqImportConflictException =>
                (StatusCodes.Status409Conflict, "RFQ import conflict"),
            RfqImportValidationException =>
                (StatusCodes.Status400BadRequest, "RFQ import validation failed"),
            RfqPdfExtractionException =>
                (StatusCodes.Status422UnprocessableEntity, "PDF text extraction failed"),
            _ => (0, string.Empty)
        };

        if (statusCode == 0) return false;

        await Results.Problem(
                statusCode: statusCode,
                title: title,
                detail: exception.Message)
            .ExecuteAsync(httpContext);
        return true;
    }
}
