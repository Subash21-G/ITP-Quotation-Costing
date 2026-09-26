using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/vendor-process-rates")]
public sealed class VendorProcessRatesController(VendorProcessRateService service)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendorProcessRateResponse>>> List(
        [FromQuery] int? vendorId,
        [FromQuery] int? processId,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(vendorId, processId, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendorProcessRateResponse>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var rate = await service.GetAsync(id, cancellationToken);
        return rate is null ? NotFound() : Ok(rate);
    }

    [HttpPost]
    public async Task<ActionResult<VendorProcessRateResponse>> Create(
        VendorProcessRateRequest request,
        CancellationToken cancellationToken)
    {
        var rate = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = rate.Id }, rate);
    }
}
