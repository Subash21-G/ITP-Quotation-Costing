using ITPQuotation.Api.Data;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ITPQuotation.Api.Tests;

public sealed class TestApiFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    private readonly string _databaseName = $"ITPQuotationTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.RemoveAll<IRfqPdfTextExtractor>();
            services.AddSingleton<IRfqPdfTextExtractor, TestPdfTextExtractor>();
        });
    }

    private sealed class TestPdfTextExtractor : IRfqPdfTextExtractor
    {
        public PdfTextExtractionResult Extract(Stream pdfStream) =>
            new(
                """
                RFQ No: RFQ-PDF-100
                Customer: Acme Engineering
                RFQ Date: 28/09/2026
                Item: 10
                Material No: MAT-100
                Description: Drive shaft
                Drawing No: DWG-10
                Quantity: 25 Nos
                Delivery Date: 30/10/2026
                Grade: EN8
                Dimensions: Dia 50 x 500 mm
                """,
                1);
    }
}
