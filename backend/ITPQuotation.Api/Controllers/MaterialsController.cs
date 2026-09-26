using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/materials")]
public sealed class MaterialsController(MaterialService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaterialMasterResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(search, isActive, cancellationToken));

    [HttpGet("{materialNo}")]
    public async Task<ActionResult<MaterialDetailsResponse>> Get(
        string materialNo,
        CancellationToken cancellationToken)
    {
        var material = await service.GetByMaterialNoAsync(materialNo, cancellationToken);
        return material is null ? NotFound() : Ok(material);
    }

    [HttpPost]
    public async Task<ActionResult<MaterialMasterResponse>> Create(
        MaterialMasterCreateRequest request,
        CancellationToken cancellationToken)
    {
        var material = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { materialNo = material.MaterialNo }, material);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MaterialMasterResponse>> Update(
        int id,
        MaterialMasterUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var material = await service.UpdateAsync(id, request, cancellationToken);
        return material is null ? NotFound() : Ok(material);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{id:int}/routing")]
    public async Task<ActionResult<IReadOnlyList<MaterialRoutingResponse>>> ListRouting(
        int id,
        CancellationToken cancellationToken)
    {
        var routing = await service.ListRoutingAsync(id, cancellationToken);
        return routing is null ? NotFound() : Ok(routing);
    }

    [HttpPost("{id:int}/routing")]
    public async Task<ActionResult<MaterialRoutingResponse>> CreateRouting(
        int id,
        MaterialRoutingRequest request,
        CancellationToken cancellationToken)
    {
        var route = await service.CreateRoutingAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(ListRouting), new { id }, route);
    }

    [HttpPut("{id:int}/routing/{routingId:int}")]
    public async Task<ActionResult<MaterialRoutingResponse>> UpdateRouting(
        int id,
        int routingId,
        MaterialRoutingRequest request,
        CancellationToken cancellationToken)
    {
        var route = await service.UpdateRoutingAsync(id, routingId, request, cancellationToken);
        return route is null ? NotFound() : Ok(route);
    }

    [HttpDelete("{id:int}/routing/{routingId:int}")]
    public async Task<IActionResult> DeleteRouting(
        int id,
        int routingId,
        CancellationToken cancellationToken) =>
        await service.DeleteRoutingAsync(id, routingId, cancellationToken)
            ? NoContent()
            : NotFound();
}
