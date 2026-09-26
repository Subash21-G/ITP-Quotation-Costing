using ITPQuotation.Api.Data;
using ITPQuotation.Api.DTOs.Mastersheet;
using ITPQuotation.Api.Models;
using ITPQuotation.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Tests;

public sealed class MaterialServiceTests
{
    [Fact]
    public async Task CreateMaterial_RejectsDuplicateMaterialNumber()
    {
        await using var context = CreateContext();
        var service = new MaterialService(context);
        var first = new MaterialMasterCreateRequest
        {
            MaterialNo = "11753667",
            ShortDescription = "Bearing cap"
        };
        var duplicate = new MaterialMasterCreateRequest
        {
            MaterialNo = "11753667",
            ShortDescription = "Duplicate"
        };

        await service.CreateAsync(first, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<MasterDataConflictException>(
            () => service.CreateAsync(duplicate, CancellationToken.None));
        Assert.Contains("already exists", exception.Message);
        Assert.Equal(1, await context.MaterialMasters.CountAsync());
    }

    [Fact]
    public async Task CreateRouting_RejectsDuplicateSequenceForMaterial()
    {
        await using var context = CreateContext();
        var material = new MaterialMaster { MaterialNo = "MAT-ROUTE" };
        var process = new ProcessMaster
        {
            ProcessName = "CNC",
            DefaultProcessType = MasterDataValues.InHouse
        };
        context.AddRange(material, process);
        await context.SaveChangesAsync();
        var service = new MaterialService(context);
        var request = new MaterialRoutingRequest
        {
            Sequence = 1,
            ProcessId = process.Id,
            ProcessType = MasterDataValues.InHouse,
            MachineRate = 950
        };

        await service.CreateRoutingAsync(material.Id, request, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<MasterDataConflictException>(
            () => service.CreateRoutingAsync(material.Id, request, CancellationToken.None));
        Assert.Contains("sequence 1", exception.Message);
        Assert.Equal(1, await context.MaterialRoutings.CountAsync());
    }

    [Fact]
    public async Task CreateRouting_RejectsUnknownProcess()
    {
        await using var context = CreateContext();
        var material = new MaterialMaster { MaterialNo = "MAT-UNKNOWN-PROCESS" };
        context.MaterialMasters.Add(material);
        await context.SaveChangesAsync();
        var service = new MaterialService(context);
        var request = new MaterialRoutingRequest
        {
            Sequence = 1,
            ProcessId = 999,
            ProcessType = MasterDataValues.InHouse
        };

        var exception = await Assert.ThrowsAsync<MasterDataValidationException>(
            () => service.CreateRoutingAsync(material.Id, request, CancellationToken.None));
        Assert.Equal("Process does not exist.", exception.Message);
    }

    [Fact]
    public async Task VendorRate_RequiresExistingVendorAndProcess()
    {
        await using var context = CreateContext();
        var service = new VendorProcessRateService(context);
        var request = new VendorProcessRateRequest
        {
            VendorId = 10,
            ProcessId = 20,
            RateType = MasterDataValues.PerPiece,
            Rate = 100,
            EffectiveFrom = DateTime.UtcNow
        };

        var exception = await Assert.ThrowsAsync<MasterDataValidationException>(
            () => service.CreateAsync(request, CancellationToken.None));
        Assert.Equal("Vendor does not exist.", exception.Message);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
