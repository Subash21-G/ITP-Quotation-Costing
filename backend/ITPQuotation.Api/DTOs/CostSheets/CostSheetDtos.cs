using System.ComponentModel.DataAnnotations;
using ITPQuotation.Api.Models;

namespace ITPQuotation.Api.DTOs.CostSheets;

public abstract record CostSheetWriteRequest : IValidatableObject
{
    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "9999.9999")]
    public decimal OverheadPercent { get; set; }

    [Range(typeof(decimal), "0", "9999.9999")]
    public decimal ProfitPercent { get; set; }

    [Required]
    public List<CostSheetLineRequest> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lines is null)
        {
            yield break;
        }

        if (Lines.Count > 500)
        {
            yield return new ValidationResult(
                "A cost sheet cannot contain more than 500 lines.",
                [nameof(Lines)]);
        }

        var sequences = new HashSet<int>();
        for (var index = 0; index < Lines.Count; index++)
        {
            var line = Lines[index];
            var lineResults = new List<ValidationResult>();
            Validator.TryValidateObject(
                line,
                new ValidationContext(line),
                lineResults,
                validateAllProperties: true);

            foreach (var result in lineResults)
            {
                var members = result.MemberNames.Any()
                    ? result.MemberNames.Select(member => $"{nameof(Lines)}[{index}].{member}")
                    : [$"{nameof(Lines)}[{index}]"];
                yield return new ValidationResult(result.ErrorMessage, members);
            }

            if (!CostSheetValues.Categories.Contains(
                    line.Category,
                    StringComparer.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    $"Lines[{index}].Category must be one of: " +
                    $"{string.Join(", ", CostSheetValues.Categories)}.",
                    [$"{nameof(Lines)}[{index}].{nameof(line.Category)}"]);
            }

            if (!sequences.Add(line.Sequence))
            {
                yield return new ValidationResult(
                    $"Line sequence {line.Sequence} is duplicated.",
                    [$"{nameof(Lines)}[{index}].{nameof(line.Sequence)}"]);
            }
        }
    }
}

public sealed record CostSheetCreateRequest : CostSheetWriteRequest
{
    [Range(1, int.MaxValue)]
    public int RfqItemId { get; set; }
}

public sealed record CostSheetUpdateRequest : CostSheetWriteRequest;

public sealed record CostSheetLineRequest
{
    [Range(1, int.MaxValue)]
    public int Sequence { get; set; }

    [Required, StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999999")]
    public decimal Amount { get; set; }
}

public sealed record CostSheetLineResponse(
    int Id,
    int Sequence,
    string Category,
    string? Description,
    decimal Amount);

public sealed record CostSheetResponse(
    int Id,
    int RfqItemId,
    int RfqId,
    decimal Quantity,
    decimal OverheadPercent,
    decimal ProfitPercent,
    IReadOnlyList<CostSheetLineResponse> Lines,
    IReadOnlyDictionary<string, decimal> CategoryTotals,
    decimal ManufacturingCost,
    decimal OverheadAmount,
    decimal CostAfterOverhead,
    decimal ProfitAmount,
    decimal SellingPrice,
    decimal UnitSellingPrice,
    DateTime CreatedDate,
    DateTime UpdatedDate);
