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
        var statusCode = exception switch
        {
            MasterDataConflictException => StatusCodes.Status409Conflict,
            MasterDataValidationException => StatusCodes.Status400BadRequest,
            _ => 0
        };

        if (statusCode == 0) return false;

        await Results.Problem(
                statusCode: statusCode,
                title: statusCode == StatusCodes.Status409Conflict
                    ? "Master data conflict"
                    : "Master data validation failed",
                detail: exception.Message)
            .ExecuteAsync(httpContext);
        return true;
    }
}
