using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Markup.Xaml;
using AvaloniaApplication1.Vm;
using Serilog.Events;

namespace AvaloniaApplication1.Views;

public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();

        DataContextChanged += (sender, _) =>
        {
            var vm = ((LogView)sender).DataContext as ViewModel; // but what if not 😬
            if (vm is null) return;
        var LogEventGridSource = new FlatTreeDataGridSource<LogEvent>(vm.View)
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
        LogTable.Source = LogEventGridSource;
        };
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
}
