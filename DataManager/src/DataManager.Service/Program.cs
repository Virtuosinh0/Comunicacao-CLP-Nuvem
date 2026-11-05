using DataManager.Service.Config;
using DataManager.Service.Http;
using DataManager.Service.Plc;
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

// Valida o plc-tags.json na inicialização (falha rápido se houver erro).
builder.Services.AddSingleton(sp =>
    TagMapResolver.Resolve(sp.GetRequiredService<IOptions<TagMapOptions>>().Value));

builder.Services.AddSingleton<IPlcReader, Sharp7PlcReader>();
builder.Services.AddSingleton<ITelemetrySender, LoggingTelemetrySender>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
