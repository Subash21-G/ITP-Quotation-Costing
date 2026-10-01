using ITPQuotation.Api.DTOs.RfqImports;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/rfq-imports")]
public sealed class RfqImportsController(RfqImportService service) : ControllerBase
{
    private const long MaximumPdfBytes = 10 * 1024 * 1024;

    [HttpPost("extract")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumPdfBytes)]
    public ActionResult<RfqPdfExtractionResponse> Extract(IFormFile file)
    {
        ValidateFile(file);
        using var stream = file.OpenReadStream();
        return Ok(service.Extract(Path.GetFileName(file.FileName), stream));
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<RfqImportConfirmationResponse>> Confirm(
        RfqImportConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.ConfirmAsync(request, cancellationToken);
        return Created($"/api/rfqs/{response.RfqId}", response);
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new RfqImportValidationException("Select a non-empty PDF file.");
        if (file.Length > MaximumPdfBytes)
            throw new RfqImportValidationException("PDF file size cannot exceed 10 MB.");
        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new RfqImportValidationException("Only PDF files are accepted.");

        using var stream = file.OpenReadStream();
        Span<byte> signature = stackalloc byte[5];
        if (stream.Read(signature) != signature.Length || !signature.SequenceEqual("%PDF-"u8))
            throw new RfqImportValidationException("The uploaded file is not a valid PDF.");
    }
}
