using RePCC.Worker.Windows;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "RePCC Worker";
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
