using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/integrations/potracker/mastersheet")]
public sealed class PoTrackerMastersheetController(PoTrackerMastersheetService service) : ControllerBase
{
    [HttpGet("process-options")]
    public async Task<ActionResult<IReadOnlyList<PoTrackerProcessOption>>> GetProcessOptions(
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetProcessOptionsAsync(cancellationToken));
        }
        catch (PoTrackerMastersheetException exception)
        {
            return StatusCode(exception.StatusCode, new { error = exception.Message });
        }
    }
}
