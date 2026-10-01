using ITPQuotation.Api.DTOs.Quotations;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/quotations")]
public sealed class QuotationsController(QuotationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuotationResponse>>> List(
        [FromQuery] int? customerId,
        [FromQuery] string? status,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(customerId, status, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuotationResponse>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var quotation = await service.GetAsync(id, cancellationToken);
        return quotation is null ? NotFound() : Ok(quotation);
    }

    [HttpPost]
    public async Task<ActionResult<QuotationResponse>> Create(
        QuotationCreateRequest request,
        CancellationToken cancellationToken)
    {
        var quotation = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = quotation.Id }, quotation);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<QuotationResponse>> Revise(
        int id,
        QuotationUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var quotation = await service.UpdateAsync(id, request, cancellationToken);
        return quotation is null ? NotFound() : Ok(quotation);
    }

    [HttpGet("{id:int}/revisions")]
    public async Task<ActionResult<IReadOnlyList<QuotationRevisionSummaryResponse>>> ListRevisions(
        int id,
        CancellationToken cancellationToken)
    {
        var revisions = await service.ListRevisionsAsync(id, cancellationToken);
        return revisions is null ? NotFound() : Ok(revisions);
    }

    [HttpGet("{id:int}/revisions/{revision:int}")]
    public async Task<ActionResult<QuotationRevisionResponse>> GetRevision(
        int id,
        int revision,
        CancellationToken cancellationToken)
    {
        var result = await service.GetRevisionAsync(id, revision, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
