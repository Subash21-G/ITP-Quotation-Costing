using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/metals")]
public sealed class MetalsController(MetalMaterialService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MetalMaterialResponse>>> List(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(isActive, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MetalMaterialResponse>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var metal = await service.GetAsync(id, cancellationToken);
        return metal is null ? NotFound() : Ok(metal);
    }

    [HttpPost]
    public async Task<ActionResult<MetalMaterialResponse>> Create(
        MetalMaterialRequest request,
        CancellationToken cancellationToken)
    {
        var metal = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = metal.Id }, metal);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MetalMaterialResponse>> Update(
        int id,
        MetalMaterialRequest request,
        CancellationToken cancellationToken)
    {
        var metal = await service.UpdateAsync(id, request, cancellationToken);
        return metal is null ? NotFound() : Ok(metal);
    }
}
