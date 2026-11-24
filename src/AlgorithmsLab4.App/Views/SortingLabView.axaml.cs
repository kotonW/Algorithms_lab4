using AlgorithmsLab4.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.Specialized;

namespace AlgorithmsLab4.App.Views;

public partial class SortingLabView : UserControl
{
    private SortingLabViewModel? _viewModel;

    public SortingLabView()
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

        _viewModel = DataContext as SortingLabViewModel;
        if (_viewModel is not null)
        {
            _viewModel.Log.CollectionChanged += OnLogChanged;
        }
    }

    private void OnLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || StepLogList.ItemCount == 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (StepLogList.ItemCount > 0)
            {
                StepLogList.ScrollIntoView(StepLogList.ItemCount - 1);
            }
        });
    }
}
