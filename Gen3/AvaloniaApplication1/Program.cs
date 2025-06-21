using System;
using System.Collections.Generic;
using Avalonia;
using AvaloniaApplication1.Logging;
using Merviche.Logging;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace AvaloniaApplication1;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseR3()
            .UseZLogger(App.LogsProvider)
            .AfterSetup(_ =>
            {
                var scope = Log.Logger.With("foo", "bar");
                scope.LogTrace("App setup complete!");
                scope.LogDebug("App setup complete!");
                scope.LogInformation("App setup complete!");
                scope.LogWarning("App setup complete!");
                scope.LogError("App setup complete!");
            });
}
