using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITPQuotation.Api.Controllers;

[ApiController]
[Route("api/processes")]
public sealed class ProcessesController(ProcessMasterService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProcessMasterResponse>>> List(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(isActive, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProcessMasterResponse>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var process = await service.GetAsync(id, cancellationToken);
        return process is null ? NotFound() : Ok(process);
    }

    [HttpPost]
    public async Task<ActionResult<ProcessMasterResponse>> Create(
        ProcessMasterRequest request,
        CancellationToken cancellationToken)
    {
        var process = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = process.Id }, process);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProcessMasterResponse>> Update(
        int id,
        ProcessMasterRequest request,
        CancellationToken cancellationToken)
    {
        var process = await service.UpdateAsync(id, request, cancellationToken);
        return process is null ? NotFound() : Ok(process);
    }
}
