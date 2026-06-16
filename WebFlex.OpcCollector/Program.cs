using WebFlex.OpcCollector;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options => {
    options.ServiceName = "WebFlex OPC Collector";
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();