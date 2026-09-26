using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Services;

public sealed class ProcessMasterService(ApplicationDbContext context)
{
    public async Task<IReadOnlyList<ProcessMasterResponse>> ListAsync(
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = context.ProcessMasters.AsNoTracking();
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        var entities = await query
            .OrderBy(x => x.ProcessName)
            .ToListAsync(cancellationToken);
        return entities.Select(x => x.ToResponse()).ToList();
    }

    public async Task<ProcessMasterResponse?> GetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var entity = await context.ProcessMasters
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity?.ToResponse();
    }

    public async Task<ProcessMasterResponse> CreateAsync(
        ProcessMasterRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = new ProcessMaster { CreatedDate = now, UpdatedDate = now };
        Apply(entity, request);
        context.ProcessMasters.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToResponse();
    }

    public async Task<ProcessMasterResponse?> UpdateAsync(
        int id,
        ProcessMasterRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await context.ProcessMasters.FindAsync([id], cancellationToken);
        if (entity is null) return null;
        Apply(entity, request);
        entity.UpdatedDate = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToResponse();
    }

    private static void Apply(ProcessMaster entity, ProcessMasterRequest request)
    {
        entity.ProcessName = request.ProcessName.Trim();
        entity.Description = Clean(request.Description);
        entity.DefaultProcessType = request.DefaultProcessType;
        entity.DefaultMachineRate = request.DefaultMachineRate;
        entity.IsActive = request.IsActive;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
