using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs;
using ITPQuotation.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/rfqs")]
public class RfqsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? customerId)
    {
        var query = context.Rfqs.AsNoTracking();
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        return Ok(await query.OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var rfq = await context.Rfqs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return rfq is null ? NotFound() : Ok(rfq);
    }

    [HttpPost]
    public async Task<IActionResult> Create(RfqRequest request)
    {
        if (!await context.Customers.AnyAsync(x => x.Id == request.CustomerId))
            return BadRequest("Customer does not exist.");
        var rfq = new Rfq();
        Apply(rfq, request);
        context.Rfqs.Add(rfq);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = rfq.Id }, rfq);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RfqRequest request)
    {
        var rfq = await context.Rfqs.FindAsync(id);
        if (rfq is null) return NotFound();
        if (!await context.Customers.AnyAsync(x => x.Id == request.CustomerId))
            return BadRequest("Customer does not exist.");
        Apply(rfq, request);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rfq = await context.Rfqs.FindAsync(id);
        if (rfq is null) return NotFound();
        if (await context.Quotations.AnyAsync(x => x.RfqId == id))
            return Conflict("RFQ has quotations and cannot be deleted.");
        context.Rfqs.Remove(rfq);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(Rfq rfq, RfqRequest request)
    {
        rfq.RfqNumber = request.RfqNumber.Trim();
        rfq.CustomerId = request.CustomerId;
        rfq.RfqDate = request.RfqDate;
        rfq.Status = request.Status;
    }
}
