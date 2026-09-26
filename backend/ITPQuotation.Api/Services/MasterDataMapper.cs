using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Models;

namespace ITPQuotation.Api.Services;

internal static class MasterDataMapper
{
    public static MetalMaterialResponse ToResponse(this MetalMaterial entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Grade,
            entity.DensityKgM3,
            entity.DefaultRatePerKg,
            entity.IsActive,
            entity.CreatedDate,
            entity.UpdatedDate);

    public static ProcessMasterResponse ToResponse(this ProcessMaster entity) =>
        new(
            entity.Id,
            entity.ProcessName,
            entity.Description,
            entity.DefaultProcessType,
            entity.DefaultMachineRate,
            entity.IsActive,
            entity.CreatedDate,
            entity.UpdatedDate);

    public static VendorResponse ToResponse(this Vendor entity) =>
        new(
            entity.Id,
            entity.VendorName,
            entity.ContactPerson,
            entity.Phone,
            entity.Email,
            entity.Address,
            entity.IsActive,
            entity.CreatedDate,
            entity.UpdatedDate);

    public static MaterialMasterResponse ToResponse(this MaterialMaster entity) =>
        new(
            entity.Id,
            entity.MaterialNo,
            entity.ShortDescription,
            entity.Grade,
            entity.DrawingCode,
            entity.DrawingVersion,
            entity.FinishSize,
            entity.MetalMaterialId,
            entity.RawMaterialShape,
            entity.RawMaterialSize,
            entity.Notes,
            entity.IsActive,
            entity.CreatedDate,
            entity.UpdatedDate,
            entity.MetalMaterial?.ToResponse());

    public static MaterialRoutingResponse ToResponse(this MaterialRouting entity) =>
        new(
            entity.Id,
            entity.MaterialMasterId,
            entity.Sequence,
            entity.ProcessId,
            entity.Process.ProcessName,
            entity.ProcessType,
            entity.VendorId,
            entity.Vendor?.VendorName,
            entity.MachineRate,
            entity.SetupTimeHours,
            entity.CycleTimeHours,
            entity.RatePerPiece,
            entity.Notes);

    public static VendorProcessRateResponse ToResponse(this VendorProcessRate entity) =>
        new(
            entity.Id,
            entity.VendorId,
            entity.Vendor.VendorName,
            entity.ProcessId,
            entity.Process.ProcessName,
            entity.RateType,
            entity.Rate,
            entity.EffectiveFrom,
            entity.EffectiveTo,
            entity.Notes);
}
