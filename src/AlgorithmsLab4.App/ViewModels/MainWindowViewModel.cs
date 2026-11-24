using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlgorithmsLab4.App.ViewModels;

/// <summary>Главная ViewModel: три задания и переключение темы.</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isDarkTheme = true;

    public MainWindowViewModel()
    {
        Sorting = new SortingLabViewModel();
        ExternalSort = new ExternalSortViewModel();
        Words = new WordLabViewModel();
    }

    public SortingLabViewModel Sorting { get; }

    public ExternalSortViewModel ExternalSort { get; }

    public WordLabViewModel Words { get; }

    public void StopAll()
    {
        Sorting.Stop();
        ExternalSort.Stop();
        Words.Stop();
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (Application.Current is { } application)
        {
            application.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}
