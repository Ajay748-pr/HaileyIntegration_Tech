using Azure.Monitor.OpenTelemetry.Exporter;
using HaileyWebhook.Client;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<HaileyClient>(sp =>
    new HaileyClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<IntegrationClient>(sp =>
    new IntegrationClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        sp.GetRequiredService<IConfiguration>()));

builder.Build().Run();
