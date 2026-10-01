namespace ITPQuotation.Api.Models;

public static class CostSheetValues
{
    public const string RawMaterial = "Raw Material";
    public const string Cutting = "Cutting";
    public const string Turning = "Turning";
    public const string Cnc = "CNC";
    public const string Milling = "Milling";
    public const string Vtl = "VTL";
    public const string Drilling = "Drilling";
    public const string Grinding = "Grinding";
    public const string WireCutting = "Wire Cutting";
    public const string HeatTreatment = "Heat Treatment";
    public const string Welding = "Welding";
    public const string Painting = "Painting";
    public const string Outsource = "Outsource";
    public const string Tooling = "Tooling";
    public const string Inspection = "Inspection";
    public const string Packing = "Packing";
    public const string Transport = "Transport";
    public const string Other = "Other";

    public static readonly string[] Categories =
    [
        RawMaterial,
        Cutting,
        Turning,
        Cnc,
        Milling,
        Vtl,
        Drilling,
        Grinding,
        WireCutting,
        HeatTreatment,
        Welding,
        Painting,
        Outsource,
        Tooling,
        Inspection,
        Packing,
        Transport,
        Other
    ];
}
