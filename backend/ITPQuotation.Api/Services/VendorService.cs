using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Services;

public sealed class VendorService(ApplicationDbContext context)
{
    public async Task<IReadOnlyList<VendorResponse>> ListAsync(
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = context.Vendors.AsNoTracking();
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        var entities = await query
            .OrderBy(x => x.VendorName)
            .ToListAsync(cancellationToken);
        return entities.Select(x => x.ToResponse()).ToList();
    }

    public async Task<VendorResponse?> GetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var entity = await context.Vendors
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity?.ToResponse();
    }

    public async Task<VendorResponse> CreateAsync(
        VendorRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = new Vendor { CreatedDate = now, UpdatedDate = now };
        Apply(entity, request);
        context.Vendors.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToResponse();
    }

    public async Task<VendorResponse?> UpdateAsync(
        int id,
        VendorRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Vendors.FindAsync([id], cancellationToken);
        if (entity is null) return null;
        Apply(entity, request);
        entity.UpdatedDate = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToResponse();
    }

    private static void Apply(Vendor entity, VendorRequest request)
    {
        entity.VendorName = request.VendorName.Trim();
        entity.ContactPerson = Clean(request.ContactPerson);
        entity.Phone = Clean(request.Phone);
        entity.Email = Clean(request.Email);
        entity.Address = Clean(request.Address);
        entity.IsActive = request.IsActive;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
