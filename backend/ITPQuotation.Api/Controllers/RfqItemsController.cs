using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs;
using ITPQuotation.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/rfqs/{rfqId:int}/items")]
public class RfqItemsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int rfqId)
    {
        if (!await context.Rfqs.AnyAsync(x => x.Id == rfqId)) return NotFound();
        return Ok(await context.RfqItems.AsNoTracking().Where(x => x.RfqId == rfqId).OrderBy(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int rfqId, int id)
    {
        var item = await context.RfqItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.RfqId == rfqId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int rfqId, RfqItemRequest request)
    {
        if (!await context.Rfqs.AnyAsync(x => x.Id == rfqId)) return NotFound();
        if (decimal.Round(request.Quantity, 3) != request.Quantity)
            return BadRequest("Quantity supports up to three decimal places.");
        var item = new RfqItem { RfqId = rfqId };
        Apply(item, request);
        context.RfqItems.Add(item);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { rfqId, id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int rfqId, int id, RfqItemRequest request)
    {
        var item = await context.RfqItems.SingleOrDefaultAsync(x => x.Id == id && x.RfqId == rfqId);
        if (item is null) return NotFound();
        if (decimal.Round(request.Quantity, 3) != request.Quantity)
            return BadRequest("Quantity supports up to three decimal places.");
        Apply(item, request);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int rfqId, int id)
    {
        var item = await context.RfqItems.SingleOrDefaultAsync(x => x.Id == id && x.RfqId == rfqId);
        if (item is null) return NotFound();
        context.RfqItems.Remove(item);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(RfqItem item, RfqItemRequest request)
    {
        item.LineItem = request.LineItem?.Trim();
        item.MaterialNo = request.MaterialNo.Trim();
        item.Description = request.Description?.Trim();
        item.DrawingNo = request.DrawingNo?.Trim();
        item.Quantity = request.Quantity;
        item.Unit = request.Unit.Trim();
        item.DeliveryDate = request.DeliveryDate;
    }
}
