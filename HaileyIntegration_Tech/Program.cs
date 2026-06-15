using HaileyIntegration.Tech.Models.Dto;
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

// Quinyx sync handlers
builder.Services.AddScoped<HaileyIntegration.Tech.Quinyx.QuinyxEmployeeUpdater>();
builder.Services.AddScoped<HaileyIntegration.Tech.Quinyx.QuinyxAgreementUpdater>();

// Downstream services — each gets its own named HttpClient for independent BaseAddress + retry config
builder.Services
    .AddHttpClient<IQuinyxService, QuinyxService>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Quinyx:BaseUrl"]
            ?? throw new InvalidOperationException("Quinyx:BaseUrl is required."));
    })
    .Services
    .AddSingleton(_ =>
        new QuinyxOptions(
            builder.Configuration["Quinyx:ApiKey"]
            ?? throw new InvalidOperationException("Quinyx:ApiKey is required.")));

builder.Services
    .AddHttpClient<IIdentityProvisioningService, IdentityProvisioningService>();

builder.Services
    .AddHttpClient<IVismaService, VismaService>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Visma:BaseUrl"]
            ?? throw new InvalidOperationException("Visma:BaseUrl is required."));
    })
    .Services
    .AddSingleton(_ =>
        new VismaOptions(
            builder.Configuration["Visma:BearerToken"]
            ?? throw new InvalidOperationException("Visma:BearerToken is required.")));

builder.Build().Run();
