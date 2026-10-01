using ITPQuotation.Api.DTOs.CostSheets;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/cost-sheets")]
public sealed class CostSheetsController(CostSheetService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CostSheetResponse>>> List(
        [FromQuery] int? rfqItemId,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(rfqItemId, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CostSheetResponse>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var costSheet = await service.GetAsync(id, cancellationToken);
        return costSheet is null ? NotFound() : Ok(costSheet);
    }

    [HttpGet("by-rfq-item/{rfqItemId:int}")]
    public async Task<ActionResult<CostSheetResponse>> GetByRfqItem(
        int rfqItemId,
        CancellationToken cancellationToken)
    {
        var costSheet = await service.GetByRfqItemAsync(rfqItemId, cancellationToken);
        return costSheet is null ? NotFound() : Ok(costSheet);
    }

    [HttpPost]
    public async Task<ActionResult<CostSheetResponse>> Create(
        CostSheetCreateRequest request,
        CancellationToken cancellationToken)
    {
        var costSheet = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = costSheet.Id }, costSheet);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CostSheetResponse>> Update(
        int id,
        CostSheetUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var costSheet = await service.UpdateAsync(id, request, cancellationToken);
        return costSheet is null ? NotFound() : Ok(costSheet);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
