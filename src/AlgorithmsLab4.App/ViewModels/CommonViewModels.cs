using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlgorithmsLab4.App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
}

/// <summary>Цвета визуализации и журнала. Одинаково читаются в тёмной и светлой теме.</summary>
public static class AppPalette
{
    public static readonly IBrush Bar = new SolidColorBrush(Color.Parse("#4F9CF9"));
    public static readonly IBrush Compare = new SolidColorBrush(Color.Parse("#F2B84B"));
    public static readonly IBrush Swap = new SolidColorBrush(Color.Parse("#F06A6A"));
    public static readonly IBrush Pivot = new SolidColorBrush(Color.Parse("#B58CFF"));
    public static readonly IBrush Sorted = new SolidColorBrush(Color.Parse("#55D187"));
    public static readonly IBrush Muted = new SolidColorBrush(Color.Parse("#8E9AAF"));
    public static readonly IBrush Accent = new SolidColorBrush(Color.Parse("#4F9CF9"));
    public static readonly IBrush Good = new SolidColorBrush(Color.Parse("#55D187"));
    public static readonly IBrush Warn = new SolidColorBrush(Color.Parse("#F2B84B"));
    public static readonly IBrush Bad = new SolidColorBrush(Color.Parse("#F06A6A"));
}

public sealed record LogEntryViewModel(string Text, IBrush Foreground);

public sealed partial class BarViewModel : ObservableObject
{
    private const double ChartHeight = 248;

    [ObservableProperty]
    private int _value;

    [ObservableProperty]
    private double _barHeight;

    [ObservableProperty]
    private bool _showLabel;

    [ObservableProperty]
    private IBrush _fill = AppPalette.Bar;

    public BarViewModel(int value, int maxValue, bool showLabel)
    {
        ShowLabel = showLabel;
        Update(value, maxValue);
    }

    public void Update(int value, int maxValue)
    {
        Value = value;
        BarHeight = Math.Max(4, value / (double)Math.Max(1, maxValue) * ChartHeight);
    }
}
