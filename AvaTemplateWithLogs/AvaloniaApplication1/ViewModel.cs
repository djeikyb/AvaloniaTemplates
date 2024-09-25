using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using ObservableCollections;
using R3;
using Serilog;
using Serilog.Events;

namespace AvaloniaApplication1;

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

        var view = App.LogsSink.Logs.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        LogEventGridSource = new FlatTreeDataGridSource<LogEvent>(view)
        {
            Columns =
            {
                new TextColumn<LogEvent, string>("level", x => ToString(x.Level)),
                new TextColumn<LogEvent, string>("time", x => x.Timestamp.LocalDateTime.ToString("hh:mm:ss:fff")),
                new TextColumn<LogEvent, string>("message template", x => x.MessageTemplate.Text),
            },
        };
        ((ITreeDataGridSource)LogEventGridSource).SortBy(LogEventGridSource.Columns[1], ListSortDirection.Descending);
        LogEventGridSource.RowSelection!.SingleSelect = false;
    }

    private static string ToString(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => "vrb",
            LogEventLevel.Debug => "dbg",
            LogEventLevel.Information => "inf",
            LogEventLevel.Warning => "wrn",
            LogEventLevel.Error => "err",
            LogEventLevel.Fatal => "ftl",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
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
}
