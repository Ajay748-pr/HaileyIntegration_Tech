using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Core services
//builder.Services.AddScoped<IEmployeeFilterService, EmployeeFilterService>();
//builder.Services.AddScoped<IEmployeeMappingService, EmployeeMappingService>();

// Quinyx sync handlers
builder.Services.AddScoped<HaileyIntegration.Tech.Quinyx.QuinyxEmployeeUpdater>();
builder.Services.AddScoped<HaileyIntegration.Tech.Quinyx.QuinyxAgreementUpdater>();
builder.Services.AddScoped<HaileyIntegration.Tech.Quinyx.QuinyxSalaryUpdater>();
builder.Services.AddScoped<HaileyIntegration.Tech.Quinyx.QuinyxEmployeeDeactivate>();

// Downstream services — each gets its own named HttpClient for independent BaseAddress + retry config
builder.Services
    .AddHttpClient<IQuinyxService, QuinyxService>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["QuinyxBaseUrl"]
            ?? throw new InvalidOperationException("QuinyxBaseUrl is required."));
    })
    .Services
    .AddSingleton(_ =>
        new QuinyxOptions(
            builder.Configuration["QuinyxApiKey"],
            builder.Configuration["QuinyxGroups"]));

//builder.Services
//    .AddHttpClient<IIdentityProvisioningService, IdentityProvisioningService>();

builder.Services
    .AddHttpClient<IVismaService, VismaService>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["VismaBaseUrl"]
            ?? throw new InvalidOperationException("VismaBaseUrl is required."));
    })
    .Services
    .AddSingleton(_ =>
        new VismaOptions(
            builder.Configuration["Visma:BearerToken"]
            ?? throw new InvalidOperationException("Visma:BearerToken is required.")));

builder.Build().Run();
