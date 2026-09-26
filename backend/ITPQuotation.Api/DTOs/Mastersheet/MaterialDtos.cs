using System.ComponentModel.DataAnnotations;

namespace ITPQuotation.Api.DTOs.Mastersheet;

public class MaterialMasterRequest
{
    [Required, StringLength(100)]
    public string MaterialNo { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ShortDescription { get; set; }

    [StringLength(100)]
    public string? Grade { get; set; }

    [StringLength(200)]
    public string? DrawingCode { get; set; }

    [StringLength(50)]
    public string? DrawingVersion { get; set; }

    [StringLength(200)]
    public string? FinishSize { get; set; }

    public int? MetalMaterialId { get; set; }

    [StringLength(50)]
    public string? RawMaterialShape { get; set; }

    [StringLength(200)]
    public string? RawMaterialSize { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class MaterialMasterCreateRequest : MaterialMasterRequest;
public sealed class MaterialMasterUpdateRequest : MaterialMasterRequest;

public sealed record MaterialMasterResponse(
    int Id,
    string MaterialNo,
    string? ShortDescription,
    string? Grade,
    string? DrawingCode,
    string? DrawingVersion,
    string? FinishSize,
    int? MetalMaterialId,
    string? RawMaterialShape,
    string? RawMaterialSize,
    string? Notes,
    bool IsActive,
    DateTime CreatedDate,
    DateTime UpdatedDate,
    MetalMaterialResponse? Metal);

public sealed record MaterialDetailsResponse(
    MaterialMasterResponse Material,
    IReadOnlyList<MaterialRoutingResponse> Routing);

public sealed record MaterialLookupResponse(
    bool MasterExists,
    string MaterialNo,
    MaterialDetailsResponse? Master);
