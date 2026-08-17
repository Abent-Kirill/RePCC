using RePCC.ViewModels;

namespace RePCC;

internal static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MauiProgram).Assembly));
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<DataBaseContext>();
        builder.Services.AddSingleton<ComputersRepository>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        return app;
    }
}
