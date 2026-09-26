using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/vendors")]
public sealed class VendorsController(VendorService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendorResponse>>> List(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(isActive, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendorResponse>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var vendor = await service.GetAsync(id, cancellationToken);
        return vendor is null ? NotFound() : Ok(vendor);
    }

    [HttpPost]
    public async Task<ActionResult<VendorResponse>> Create(
        VendorRequest request,
        CancellationToken cancellationToken)
    {
        var vendor = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = vendor.Id }, vendor);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VendorResponse>> Update(
        int id,
        VendorRequest request,
        CancellationToken cancellationToken)
    {
        var vendor = await service.UpdateAsync(id, request, cancellationToken);
        return vendor is null ? NotFound() : Ok(vendor);
    }
}
