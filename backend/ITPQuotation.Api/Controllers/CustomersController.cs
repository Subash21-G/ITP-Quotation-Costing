using ITPQuotation.Api.Data;
using ITPQuotation.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    //GET: api/customers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
    {
        var customers = await _context.Customers
        .OrderByDescending(x => x.Id)
        .ToListAsync();

        return Ok(customers);

    }

    //GET: api/customers/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer is null)

        {
            return NotFound();

        }

        return Ok(customer);
    }
    //POST: api/customers
    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
    {
        customer.Id = 0;
        customer.CreatedDate = DateTime.UtcNow;

        _context.Customers.Add(customer);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = customer.Id },
            customer);

    }
    //PUT: api/customers/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(
        int id,
        Customer customer)
    {
        if (id != customer.Id)
        {
            return BadRequest("Customer ID mismatch.");

        }

        var existingCustomer =
        await _context.Customers.FindAsync(id);

        if (existingCustomer is null)
        {
            return NotFound();
        }

        existingCustomer.CustomerName = customer.CustomerName;
        existingCustomer.ContactPerson = customer.ContactPerson;
        existingCustomer.Email = customer.Email;
        existingCustomer.Phone = customer.Phone;
        existingCustomer.Address = customer.Address;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    //DELETE: api/customers/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer =
        await _context.Customers.FindAsync(id);

        if (customer is null)
        {
            return NotFound();
        }

        if (await _context.Rfqs.AnyAsync(x => x.CustomerId == id))
            return Conflict("Customer has RFQs and cannot be deleted.");
        _context.Customers.Remove(customer);

        await _context.SaveChangesAsync();

        return NoContent();
    }

}
