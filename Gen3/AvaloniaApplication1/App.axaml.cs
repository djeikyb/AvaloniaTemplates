using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaApplication1.Vm;

namespace AvaloniaApplication1;

public class App : Application
{
    static App()
    {
        LogsProvider = new ObservableLogProvider(14);
    }

    public static ObservableLogProvider LogsProvider { get; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = new ViewModel() };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
