using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HotAvalonia;

namespace AvaloniaApplication1;

public class App : Application
{
    static App()
    {
        LogsSink = new ObservableLogEventSink(14);
    }

    public static ObservableLogEventSink LogsSink { get; }

    public override void Initialize()
    {
        this.EnableHotReload(); // Ensure this line **precedes** `AvaloniaXamlLoader.Load(this);`
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
