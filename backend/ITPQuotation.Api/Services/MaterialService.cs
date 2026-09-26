using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Services;

public sealed class MaterialService(ApplicationDbContext context)
{
    public async Task<IReadOnlyList<MaterialMasterResponse>> ListAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = context.MaterialMasters
            .AsNoTracking()
            .Include(x => x.MetalMaterial)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.MaterialNo.Contains(term) ||
                (x.ShortDescription != null && x.ShortDescription.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var entities = await query
            .OrderBy(x => x.MaterialNo)
            .ToListAsync(cancellationToken);
        return entities.Select(x => x.ToResponse()).ToList();
    }

    public async Task<MaterialDetailsResponse?> GetByMaterialNoAsync(
        string materialNo,
        CancellationToken cancellationToken)
    {
        var normalized = materialNo.Trim();
        var material = await context.MaterialMasters
            .AsNoTracking()
            .Include(x => x.MetalMaterial)
            .SingleOrDefaultAsync(x => x.MaterialNo == normalized, cancellationToken);

        return material is null
            ? null
            : new MaterialDetailsResponse(
                material.ToResponse(),
                await ListRoutingInternalAsync(material.Id, cancellationToken));
    }

    public async Task<MaterialMasterResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var material = await context.MaterialMasters
            .AsNoTracking()
            .Include(x => x.MetalMaterial)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return material?.ToResponse();
    }

    public async Task<MaterialMasterResponse> CreateAsync(
        MaterialMasterCreateRequest request,
        CancellationToken cancellationToken)
    {
        var materialNo = request.MaterialNo.Trim();
        await ValidateMaterialAsync(materialNo, request.MetalMaterialId, null, cancellationToken);

        var now = DateTime.UtcNow;
        var material = new MaterialMaster
        {
            CreatedDate = now,
            UpdatedDate = now
        };
        Apply(material, request, materialNo);
        context.MaterialMasters.Add(material);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new MasterDataConflictException(
                $"Material number '{materialNo}' already exists.",
                exception);
        }

        await context.Entry(material).Reference(x => x.MetalMaterial).LoadAsync(cancellationToken);
        return material.ToResponse();
    }

    public async Task<MaterialMasterResponse?> UpdateAsync(
        int id,
        MaterialMasterUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var material = await context.MaterialMasters
            .Include(x => x.MetalMaterial)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (material is null) return null;
        var oldMetalMaterialId = material.MetalMaterialId;

        var materialNo = request.MaterialNo.Trim();
        await ValidateMaterialAsync(materialNo, request.MetalMaterialId, id, cancellationToken);
        Apply(material, request, materialNo);
        material.UpdatedDate = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new MasterDataConflictException(
                $"Material number '{materialNo}' already exists.",
                exception);
        }

        if (oldMetalMaterialId != material.MetalMaterialId)
        {
            material.MetalMaterial = material.MetalMaterialId.HasValue
                ? await context.MetalMaterials
                    .AsNoTracking()
                    .SingleAsync(x => x.Id == material.MetalMaterialId.Value, cancellationToken)
                : null;
        }

        return material.ToResponse();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var material = await context.MaterialMasters.FindAsync([id], cancellationToken);
        if (material is null) return false;

        if (await context.MaterialRoutings.AnyAsync(
                x => x.MaterialMasterId == id,
                cancellationToken))
        {
            throw new MasterDataConflictException(
                "Material has routing records and cannot be deleted.");
        }

        context.MaterialMasters.Remove(material);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<MaterialRoutingResponse>?> ListRoutingAsync(
        int materialId,
        CancellationToken cancellationToken)
    {
        if (!await context.MaterialMasters.AnyAsync(
                x => x.Id == materialId,
                cancellationToken))
        {
            return null;
        }

        return await ListRoutingInternalAsync(materialId, cancellationToken);
    }

    public async Task<MaterialRoutingResponse> CreateRoutingAsync(
        int materialId,
        MaterialRoutingRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateRoutingAsync(materialId, request, null, cancellationToken);
        var route = new MaterialRouting { MaterialMasterId = materialId };
        Apply(route, request);
        context.MaterialRoutings.Add(route);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new MasterDataConflictException(
                $"Routing sequence {request.Sequence} already exists for this material.",
                exception);
        }

        await LoadRoutingReferencesAsync(route, cancellationToken);
        return route.ToResponse();
    }

    public async Task<MaterialRoutingResponse?> UpdateRoutingAsync(
        int materialId,
        int routingId,
        MaterialRoutingRequest request,
        CancellationToken cancellationToken)
    {
        var route = await context.MaterialRoutings
            .SingleOrDefaultAsync(
                x => x.Id == routingId && x.MaterialMasterId == materialId,
                cancellationToken);
        if (route is null) return null;

        await ValidateRoutingAsync(materialId, request, routingId, cancellationToken);
        Apply(route, request);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new MasterDataConflictException(
                $"Routing sequence {request.Sequence} already exists for this material.",
                exception);
        }

        await LoadRoutingReferencesAsync(route, cancellationToken);
        return route.ToResponse();
    }

    public async Task<bool> DeleteRoutingAsync(
        int materialId,
        int routingId,
        CancellationToken cancellationToken)
    {
        var route = await context.MaterialRoutings.SingleOrDefaultAsync(
            x => x.Id == routingId && x.MaterialMasterId == materialId,
            cancellationToken);
        if (route is null) return false;

        context.MaterialRoutings.Remove(route);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MaterialLookupResponse?> LookupForRfqItemAsync(
        int rfqId,
        int itemId,
        CancellationToken cancellationToken)
    {
        var item = await context.RfqItems
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == itemId && x.RfqId == rfqId,
                cancellationToken);
        if (item is null) return null;

        var details = await GetByMaterialNoAsync(item.MaterialNo, cancellationToken);
        return new MaterialLookupResponse(details is not null, item.MaterialNo, details);
    }

    private async Task ValidateMaterialAsync(
        string materialNo,
        int? metalMaterialId,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (await context.MaterialMasters.AnyAsync(
                x => x.MaterialNo == materialNo && x.Id != currentId,
                cancellationToken))
        {
            throw new MasterDataConflictException(
                $"Material number '{materialNo}' already exists.");
        }

        if (metalMaterialId.HasValue &&
            !await context.MetalMaterials.AnyAsync(
                x => x.Id == metalMaterialId.Value,
                cancellationToken))
        {
            throw new MasterDataValidationException("Metal material does not exist.");
        }
    }

    private async Task ValidateRoutingAsync(
        int materialId,
        MaterialRoutingRequest request,
        int? currentRoutingId,
        CancellationToken cancellationToken)
    {
        if (!await context.MaterialMasters.AnyAsync(
                x => x.Id == materialId,
                cancellationToken))
        {
            throw new MasterDataValidationException("Material does not exist.");
        }

        if (!await context.ProcessMasters.AnyAsync(
                x => x.Id == request.ProcessId,
                cancellationToken))
        {
            throw new MasterDataValidationException("Process does not exist.");
        }

        if (request.VendorId.HasValue &&
            !await context.Vendors.AnyAsync(
                x => x.Id == request.VendorId.Value,
                cancellationToken))
        {
            throw new MasterDataValidationException("Vendor does not exist.");
        }

        if (await context.MaterialRoutings.AnyAsync(
                x => x.MaterialMasterId == materialId &&
                     x.Sequence == request.Sequence &&
                     x.Id != currentRoutingId,
                cancellationToken))
        {
            throw new MasterDataConflictException(
                $"Routing sequence {request.Sequence} already exists for this material.");
        }
    }

    private async Task<IReadOnlyList<MaterialRoutingResponse>> ListRoutingInternalAsync(
        int materialId,
        CancellationToken cancellationToken)
    {
        var entities = await context.MaterialRoutings
            .AsNoTracking()
            .Include(x => x.Process)
            .Include(x => x.Vendor)
            .Where(x => x.MaterialMasterId == materialId)
            .OrderBy(x => x.Sequence)
            .ToListAsync(cancellationToken);
        return entities.Select(x => x.ToResponse()).ToList();
    }

    private async Task LoadRoutingReferencesAsync(
        MaterialRouting route,
        CancellationToken cancellationToken)
    {
        await context.Entry(route).Reference(x => x.Process).LoadAsync(cancellationToken);
        if (route.VendorId.HasValue)
        {
            await context.Entry(route).Reference(x => x.Vendor).LoadAsync(cancellationToken);
        }
    }

    private static void Apply(
        MaterialMaster material,
        MaterialMasterRequest request,
        string materialNo)
    {
        material.MaterialNo = materialNo;
        material.ShortDescription = Clean(request.ShortDescription);
        material.Grade = Clean(request.Grade);
        material.DrawingCode = Clean(request.DrawingCode);
        material.DrawingVersion = Clean(request.DrawingVersion);
        material.FinishSize = Clean(request.FinishSize);
        material.MetalMaterialId = request.MetalMaterialId;
        material.RawMaterialShape = Clean(request.RawMaterialShape);
        material.RawMaterialSize = Clean(request.RawMaterialSize);
        material.Notes = Clean(request.Notes);
        material.IsActive = request.IsActive;
    }

    private static void Apply(MaterialRouting route, MaterialRoutingRequest request)
    {
        route.Sequence = request.Sequence;
        route.ProcessId = request.ProcessId;
        route.ProcessType = request.ProcessType;
        route.VendorId = request.VendorId;
        route.MachineRate = request.MachineRate;
        route.SetupTimeHours = request.SetupTimeHours;
        route.CycleTimeHours = request.CycleTimeHours;
        route.RatePerPiece = request.RatePerPiece;
        route.Notes = Clean(request.Notes);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
