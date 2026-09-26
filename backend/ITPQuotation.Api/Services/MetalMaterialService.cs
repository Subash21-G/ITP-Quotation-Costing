using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Services;

public sealed class MetalMaterialService(ApplicationDbContext context)
{
    public async Task<IReadOnlyList<MetalMaterialResponse>> ListAsync(
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = context.MetalMaterials.AsNoTracking();
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        var entities = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Grade)
            .ToListAsync(cancellationToken);
        return entities.Select(x => x.ToResponse()).ToList();
    }

    public async Task<MetalMaterialResponse?> GetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var entity = await context.MetalMaterials
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity?.ToResponse();
    }

    public async Task<MetalMaterialResponse> CreateAsync(
        MetalMaterialRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = new MetalMaterial { CreatedDate = now, UpdatedDate = now };
        Apply(entity, request);
        context.MetalMaterials.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToResponse();
    }

    public async Task<MetalMaterialResponse?> UpdateAsync(
        int id,
        MetalMaterialRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await context.MetalMaterials.FindAsync([id], cancellationToken);
        if (entity is null) return null;
        Apply(entity, request);
        entity.UpdatedDate = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToResponse();
    }

    private static void Apply(MetalMaterial entity, MetalMaterialRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Grade = Clean(request.Grade);
        entity.DensityKgM3 = request.DensityKgM3;
        entity.DefaultRatePerKg = request.DefaultRatePerKg;
        entity.IsActive = request.IsActive;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
