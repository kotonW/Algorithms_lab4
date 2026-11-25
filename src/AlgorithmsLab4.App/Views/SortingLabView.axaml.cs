using AlgorithmsLab4.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
        if (e.Action != NotifyCollectionChangedAction.Add)
        {
            return;
        }

        // Крутим только внутренний ScrollViewer журнала (а не всю страницу) и только
        // если пользователь и так смотрит на конец списка: так журнал можно спокойно читать.
        var scroll = StepLogList.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroll is null)
        {
            return;
        }

        var atBottom = scroll.Extent.Height <= scroll.Viewport.Height + 1
            || scroll.Offset.Y + scroll.Viewport.Height >= scroll.Extent.Height - 4;
        if (!atBottom)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => scroll.ScrollToEnd(), DispatcherPriority.Background);
    }
}
