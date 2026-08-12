using RePCC.Worker.Windows;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddEventLog(options =>
{
    options.SourceName = "MyShutdownService";
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
