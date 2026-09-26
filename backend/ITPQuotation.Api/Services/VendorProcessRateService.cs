using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Services;

public sealed class VendorProcessRateService(ApplicationDbContext context)
{
    public async Task<IReadOnlyList<VendorProcessRateResponse>> ListAsync(
        int? vendorId,
        int? processId,
        CancellationToken cancellationToken)
    {
        var query = context.VendorProcessRates
            .AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.Process)
            .AsQueryable();
        if (vendorId.HasValue) query = query.Where(x => x.VendorId == vendorId.Value);
        if (processId.HasValue) query = query.Where(x => x.ProcessId == processId.Value);
        var entities = await query
            .OrderBy(x => x.Vendor.VendorName)
            .ThenBy(x => x.Process.ProcessName)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);
        return entities.Select(x => x.ToResponse()).ToList();
    }

    public async Task<VendorProcessRateResponse?> GetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var entity = await context.VendorProcessRates
            .AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.Process)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity?.ToResponse();
    }

    public async Task<VendorProcessRateResponse> CreateAsync(
        VendorProcessRateRequest request,
        CancellationToken cancellationToken)
    {
        if (!await context.Vendors.AnyAsync(x => x.Id == request.VendorId, cancellationToken))
        {
            throw new MasterDataValidationException("Vendor does not exist.");
        }

        if (!await context.ProcessMasters.AnyAsync(x => x.Id == request.ProcessId, cancellationToken))
        {
            throw new MasterDataValidationException("Process does not exist.");
        }

        var entity = new VendorProcessRate
        {
            VendorId = request.VendorId,
            ProcessId = request.ProcessId,
            RateType = request.RateType,
            Rate = request.Rate,
            EffectiveFrom = request.EffectiveFrom == default
                ? DateTime.UtcNow
                : request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
        context.VendorProcessRates.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        await context.Entry(entity).Reference(x => x.Vendor).LoadAsync(cancellationToken);
        await context.Entry(entity).Reference(x => x.Process).LoadAsync(cancellationToken);
        return entity.ToResponse();
    }
}
