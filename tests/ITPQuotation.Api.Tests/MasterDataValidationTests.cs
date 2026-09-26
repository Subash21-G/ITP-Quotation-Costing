using ITPQuotation.Api.DTOs.Mastersheet;

namespace ITPQuotation.Api.Tests;

public sealed class MasterDataValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MetalDensity_MustBePositive(double density)
    {
        var request = new MetalMaterialRequest
        {
            Name = "Carbon Steel",
            DensityKgM3 = (decimal)density,
            DefaultRatePerKg = 80
        };

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.DensityKgM3)));
    }

    [Fact]
    public void MetalDensity_AcceptsValidEngineeringValue()
    {
        var request = new MetalMaterialRequest
        {
            Name = "Carbon Steel",
            DensityKgM3 = 7850,
            DefaultRatePerKg = 80
        };

        Assert.Empty(ValidationTestHelper.Validate(request));
    }

    [Fact]
    public void RoutingSequence_MustBePositive()
    {
        var request = new MaterialRoutingRequest
        {
            Sequence = 0,
            ProcessId = 1,
            ProcessType = "InHouse"
        };

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.Sequence)));
    }

    [Fact]
    public void OutsourceRouting_RequiresVendor()
    {
        var request = new MaterialRoutingRequest
        {
            Sequence = 1,
            ProcessId = 1,
            ProcessType = "Outsource"
        };

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.VendorId)));
    }

    [Fact]
    public void InHouseRouting_RejectsVendor()
    {
        var request = new MaterialRoutingRequest
        {
            Sequence = 1,
            ProcessId = 1,
            ProcessType = "InHouse",
            VendorId = 10
        };

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.VendorId)));
    }

    [Fact]
    public void VendorRate_EndDateCannotPrecedeStartDate()
    {
        var request = new VendorProcessRateRequest
        {
            VendorId = 1,
            ProcessId = 1,
            RateType = "PerPiece",
            Rate = 100,
            EffectiveFrom = new DateTime(2026, 9, 27),
            EffectiveTo = new DateTime(2026, 9, 26)
        };

        var results = ValidationTestHelper.Validate(request);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.EffectiveTo)));
    }
}
