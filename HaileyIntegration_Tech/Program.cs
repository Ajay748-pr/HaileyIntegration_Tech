using HaileyIntegration.Tech.Services;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Core services
builder.Services.AddScoped<IEmployeeFilterService, EmployeeFilterService>();
builder.Services.AddScoped<IEmployeeMappingService, EmployeeMappingService>();

// Hailey HR API — the only live integration in Phase 1
builder.Services.AddHttpClient<IHaileyApiService, HaileyApiService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Hailey:BaseUrl"]
        ?? throw new InvalidOperationException("Hailey:BaseUrl is required in local.settings.json."));
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Downstream services — not yet live; placeholder URLs used locally, real URLs added in Phase 2
builder.Services.AddHttpClient<IPrimulaService, PrimulaService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Primula:BaseUrl"] ?? "https://placeholder.invalid/"));

builder.Services.AddHttpClient<IQuinyxService, QuinyxService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Quinyx:BaseUrl"] ?? "https://placeholder.invalid/"));

builder.Services.AddHttpClient<ILearnifyService, LearnifyService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Learnify:BaseUrl"] ?? "https://placeholder.invalid/"));

builder.Services.AddHttpClient<IIdentityProvisioningService, IdentityProvisioningService>();

builder.Build().Run();
