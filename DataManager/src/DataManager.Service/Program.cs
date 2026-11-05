using DataManager.Service.Config;
using DataManager.Service.Http;
using DataManager.Service.Models;
using DataManager.Service.Services;
using Microsoft.Extensions.Options;

// ContentRoot = pasta do executável: um Serviço Windows roda com diretório atual em C:\Windows\System32.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// "JSON CLP": mapa de tags do CLP → campos da API.
builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, "Config", "plc-tags.json"),
    optional: false,
    reloadOnChange: false);

// Permite que o processo rode como Serviço Windows (e também via console para debug).
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DataManager";
});

builder.Services.Configure<PlcOptions>(builder.Configuration.GetSection(PlcOptions.SectionName));
builder.Services.Configure<TagMapOptions>(builder.Configuration.GetSection(TagMapOptions.SectionName));

// Valida catálogo de CLPs/OPs + conexões + plc-tags.json na inicialização (falha rápido se houver erro).
builder.Services.AddSingleton<IReadOnlyList<PlcStation>>(sp => TagMapResolver.Resolve(
    PlcCatalog.Plcs,
    sp.GetRequiredService<IOptions<PlcOptions>>().Value,
    sp.GetRequiredService<IOptions<TagMapOptions>>().Value));

// Api:BaseUrl preenchido → envia para a API; vazio → só registra o JSON no log.
var apiSection = builder.Configuration.GetSection(ApiOptions.SectionName);
builder.Services.Configure<ApiOptions>(apiSection);
var apiBaseUrl = apiSection.GetValue<string>(nameof(ApiOptions.BaseUrl));
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    builder.Services.AddSingleton<ITelemetrySender, LoggingTelemetrySender>();
}
else
{
    if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out _))
        throw new InvalidOperationException($"appsettings.json: Api:BaseUrl '{apiBaseUrl}' inválida.");

    builder.Services.AddSingleton<ITelemetrySender, HttpTelemetrySender>();
}
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
