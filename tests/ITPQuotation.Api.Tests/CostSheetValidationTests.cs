using ITPQuotation.Api.DTOs.CostSheets;

namespace ITPQuotation.Api.Tests;

public sealed class CostSheetValidationTests
{
    [Fact]
    public void CostSheet_WithDuplicateLineSequence_IsRejected()
    {
        var request = ValidRequest();
        request.Lines.Add(new CostSheetLineRequest
        {
            Sequence = 1,
            Category = "Packing",
            Amount = 25m
        });

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, result =>
            result.ErrorMessage!.Contains("duplicated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CostSheet_WithUnknownCategory_IsRejected()
    {
        var request = ValidRequest();
        request.Lines[0].Category = "Unapproved Category";

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, result =>
            result.ErrorMessage!.Contains("Category must be one of"));
    }

    [Fact]
    public void CostSheet_WithNegativeLineAmount_IsRejected()
    {
        var request = ValidRequest();
        request.Lines[0].Amount = -1m;

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, result =>
            result.MemberNames.Any(member => member.EndsWith("Amount", StringComparison.Ordinal)));
    }

    private static CostSheetCreateRequest ValidRequest() =>
        new()
        {
            RfqItemId = 1,
            Quantity = 10m,
            OverheadPercent = 10m,
            ProfitPercent = 15m,
            Lines =
            [
                new CostSheetLineRequest
                {
                    Sequence = 1,
                    Category = "Raw Material",
                    Amount = 1000m
                }
            ]
        };
}
