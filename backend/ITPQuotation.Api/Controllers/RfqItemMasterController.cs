using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/rfqs/{rfqId:int}/items/{itemId:int}/master")]
public sealed class RfqItemMasterController(MaterialService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MaterialLookupResponse>> Get(
        int rfqId,
        int itemId,
        CancellationToken cancellationToken)
    {
        var lookup = await service.LookupForRfqItemAsync(
            rfqId,
            itemId,
            cancellationToken);
        return lookup is null ? NotFound() : Ok(lookup);
    }
}
