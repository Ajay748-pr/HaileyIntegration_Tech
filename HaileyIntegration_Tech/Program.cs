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

// Downstream services — each gets its own named HttpClient for independent BaseAddress + retry config
builder.Services
    .AddHttpClient<IPrimulaService, PrimulaService>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Primula:BaseUrl"]
            ?? throw new InvalidOperationException("Primula:BaseUrl is required."));
    });

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
    .AddHttpClient<ILearnifyService, LearnifyService>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Learnify:BaseUrl"]
            ?? throw new InvalidOperationException("Learnify:BaseUrl is required."));
    });

builder.Services
    .AddHttpClient<IIdentityProvisioningService, IdentityProvisioningService>();

builder.Build().Run();
