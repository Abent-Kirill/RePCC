using RePCC.ViewModels;

namespace RePCC;

internal static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MauiProgram).Assembly));
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<DataBaseContext>();
        builder.Services.AddTransient<ComputersRepository>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        return app;
    }
}
