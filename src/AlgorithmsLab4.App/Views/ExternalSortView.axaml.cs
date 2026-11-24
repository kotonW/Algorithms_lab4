using AlgorithmsLab4.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System.Diagnostics;
using System.Collections.Specialized;

namespace AlgorithmsLab4.App.Views;

public partial class ExternalSortView : UserControl
{
    private ExternalSortViewModel? _viewModel;

    public ExternalSortView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.Log.CollectionChanged -= OnLogChanged;
        }

        _viewModel = DataContext as ExternalSortViewModel;
        if (_viewModel is not null)
        {
            _viewModel.Log.CollectionChanged += OnLogChanged;
        }
    }

    private async void OpenFile_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ExternalSortViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть CSV-таблицу",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CSV-таблица")
                {
                    Patterns = ["*.csv", "*.txt"],
                    MimeTypes = ["text/csv", "text/plain"],
                },
                FilePickerFileTypes.All,
            ],
        });

        if (files.Count == 0)
        {
            return;
        }

        var path = files[0].Path.LocalPath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            await viewModel.LoadFileAsync(path);
        }
    }

    private void OpenOutputFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ExternalSortViewModel viewModel)
        {
            return;
        }

        try
        {
            var path = viewModel.OutputPath;
            var directory = File.Exists(path) ? Path.GetDirectoryName(path) : path;
            if (directory is not null && Directory.Exists(directory))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = directory,
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            viewModel.Status = $"Не удалось открыть папку: {exception.Message}";
        }
    }

    private void OnLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || ExternalLogList.ItemCount == 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (ExternalLogList.ItemCount > 0)
            {
                ExternalLogList.ScrollIntoView(ExternalLogList.ItemCount - 1);
            }
        });
    }
}
