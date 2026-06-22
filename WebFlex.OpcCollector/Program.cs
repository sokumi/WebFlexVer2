using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using WebFlex.OpcCollector;
using WebFlex.OpcCollector.Data;
using WebFlex.OpcCollector.Logging;
using WebFlex.OpcCollector.Options;
using WebFlex.OpcCollector.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options => {
    options.ServiceName = "WebFlex OPC Collector";
});

builder.Logging.AddProvider(new MemoryLoggerProvider());

builder.Services.Configure<OpcCollectorOptions>(
    builder.Configuration.GetSection("OpcCollector"));

builder.Services.AddSingleton<OpcCollectorOptionState>();

builder.Services.AddDbContextFactory<WebFlexConfigDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebFlexDb")));

builder.Services.AddSingleton<OpcCollectTargetProvider>();
builder.Services.AddSingleton<OpcClientOptionState>();
builder.Services.AddSingleton<TimescaleDbWriter>();
builder.Services.AddSingleton<OpcUaSessionFactory>();
builder.Services.AddSingleton<OpcUaRuntimeService>();
builder.Services.AddSingleton<OpcRuntimeStatusService>();
builder.Services.AddSingleton<OpcRuntimeManager>();
builder.Services.AddSingleton<OpcHistoryReadService>();

builder.Services.AddHostedService<Worker>();

builder.Services.AddControllers();

var app = builder.Build();

await app.Services.GetRequiredService<OpcClientOptionState>().LoadAsync();

app.MapControllers();

app.Run();