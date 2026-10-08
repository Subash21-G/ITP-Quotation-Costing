using ITPQuotation.Api.Calculators;
using ITPQuotation.Api.Common;
using ITPQuotation.Api.Data;
using ITPQuotation.Api.Documents;
using ITPQuotation.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ITPQuotation.Api.Models;
using System.Text;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var questPdfLicenseName = builder.Configuration["QuestPdf:License"] ?? "Evaluation";
if (!Enum.TryParse<LicenseType>(questPdfLicenseName, true, out var questPdfLicense))
{
    throw new InvalidOperationException(
        "QuestPdf:License must be Evaluation, Community, Professional, or Enterprise.");
}

QuestPDF.Settings.License = questPdfLicense;

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentityCore<ApplicationUser>(options => { options.Password.RequiredLength = 8; options.User.RequireUniqueEmail = false; })
    .AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"], ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), ValidateLifetime = true });
builder.Services.AddAuthorization();

builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<MetalMaterialService>();
builder.Services.AddScoped<ProcessMasterService>();
builder.Services.AddScoped<VendorService>();
builder.Services.AddScoped<VendorProcessRateService>();
builder.Services.AddSingleton<IMetalWeightCalculator, MetalWeightCalculator>();
builder.Services.AddSingleton<IRawMaterialCostCalculator, RawMaterialCostCalculator>();
builder.Services.AddSingleton<IProcessCostCalculator, ProcessCostCalculator>();
builder.Services.AddSingleton<ICostSheetCalculator, CostSheetCalculator>();
builder.Services.AddScoped<CostSheetService>();
builder.Services.AddScoped<QuotationService>();
builder.Services.AddHttpClient<PoTrackerExportService>(client => client.Timeout = TimeSpan.FromSeconds(30))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<IQuotationPdfGenerator, QuestPdfQuotationPdfGenerator>();
builder.Services.AddSingleton<IRfqPdfTextExtractor, PdfPigRfqTextExtractor>();
builder.Services.AddSingleton<RfqTextParser>();
builder.Services.AddScoped<RfqImportService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MasterDataExceptionHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in AuthService.Roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
