using ITPQuotation.Api.Data;
using ITPQuotation.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ITPQuotation.Api.Tests;

public sealed class TestApiFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    private readonly string _databaseName = $"ITPQuotationTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.RemoveAll<IRfqPdfTextExtractor>();
            services.AddSingleton<IRfqPdfTextExtractor, TestPdfTextExtractor>();
        });
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Test Admin"), new Claim(ClaimTypes.Role, "Admin")], Scheme.Name)),
                Scheme.Name)));
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
