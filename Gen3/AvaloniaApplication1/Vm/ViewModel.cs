using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;

namespace AvaloniaApplication1.Vm;

public class ViewModel
{
    private int _index = 0;

    public ViewModel()
    {
        var logger = Log.GetLogger<ViewModel>();
        Click = new ReactiveCommand<Unit>();
        Click.Subscribe(_ =>
        {
            var next = _lines[_index++ % _lines.Length];
            logger.LogInformation($"{next}");
        });

        View = App.LogsProvider.Logs.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        TintOpacity = new(1m);
        MaterialOpacity = new(1m);
    }

    public BindableReactiveProperty<decimal> TintOpacity { get; }
    public BindableReactiveProperty<decimal> MaterialOpacity { get; }

    public ReactiveCommand<Unit> Click { get; }

    private string[] _lines =
    [
        "I have eaten",
        "the plums",
        "that were in",
        "the icebox",
        "",
        "and which",
        "you were probably",
        "saving",
        "for breakfast",
        "",
        "Forgive me",
        "they were delicious",
        "so sweet",
        "and so cold",
    ];

    public INotifyCollectionChangedSynchronizedViewList<LogEvent> View { get; }
}
