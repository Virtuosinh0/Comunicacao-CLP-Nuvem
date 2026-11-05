using ApiHost;

// ContentRoot = pasta do executável (um Serviço Windows roda com diretório atual em C:\Windows\System32).
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddWindowsService(options => options.ServiceName = "ClpApi");
builder.Services.Configure<JavaOptions>(builder.Configuration.GetSection(JavaOptions.SectionName));
builder.Services.AddHostedService<JavaProcessService>();

builder.Build().Run();
