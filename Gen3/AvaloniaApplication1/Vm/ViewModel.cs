using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using ObservableCollections;
using R3;
using Serilog;
using Serilog.Events;

namespace AvaloniaApplication1.Vm;

public class ViewModel
{
    private int _index = 0;

    public ViewModel()
    {
        var logger = Log.ForContext<ViewModel>();
        Click = new ReactiveCommand<Unit>();
        Click.Subscribe(_ =>
        {
            var next = _lines[_index++ % _lines.Length];
            logger.Information($"{next}");
        });

        View = App.LogsSink.Logs.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
    }

    public ReactiveCommand<Unit> Click { get; }
    public FlatTreeDataGridSource<LogEvent> LogEventGridSource { get; }

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
