using ITPQuotation.Api.Common;
using ITPQuotation.Api.Data;
using ITPQuotation.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<MetalMaterialService>();
builder.Services.AddScoped<ProcessMasterService>();
builder.Services.AddScoped<VendorService>();
builder.Services.AddScoped<VendorProcessRateService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MasterDataExceptionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.MapControllers();

app.Run();
