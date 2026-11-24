using AlgorithmsLab4.Core.Sorting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlgorithmsLab4.App.ViewModels;

public sealed record AlgorithmOption(
    SortAlgorithm Value,
    string Title,
    string Group,
    string Description,
    string Complexity);

/// <summary>
/// ViewModel задания 1: пошаговая визуализация четырёх алгоритмов
/// внутренней сортировки с журналом сравнений и перестановок.
/// </summary>
public sealed partial class SortingLabViewModel : ViewModelBase
{
    private const int MinArraySize = 3;
    private const int MaxArraySize = 64;

    private CancellationTokenSource? _cancellation;
    private readonly HashSet<int> _sortedIndices = [];
    private int _pivotIndex = -1;
    private int _stepNumber;

    [ObservableProperty]
    private AlgorithmOption _selectedAlgorithm;

    [ObservableProperty]
    private string _arrayText = "42, 8, 17, 3, 91, 25, 60, 14, 73, 31, 5, 88, 47, 12, 66, 29";

    [ObservableProperty]
    private int _selectedRandomSize = 20;

    [ObservableProperty]
    private double _delayMs = 140;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _status = "Выберите алгоритм и нажмите «Старт».";

    [ObservableProperty]
    private string _explanation = "Нажмите «Старт», и программа покажет каждый шаг сортировки: сравнения, перестановки и готовые элементы.";

    [ObservableProperty]
    private int _comparisons;

    [ObservableProperty]
    private int _swaps;

    [ObservableProperty]
    private int _steps;

    [ObservableProperty]
    private int _arrayLength;

    public SortingLabViewModel()
    {
        Algorithms =
        [
            new AlgorithmOption(
                SortAlgorithm.SelectSort,
                "Сортировка выбором",
                "Квадратичная группа · SelectSort",
                "На каждом проходе алгоритм находит самый маленький элемент в неотсортированной части и ставит его в начало. Поэтому после k проходов первые k элементов уже стоят на своих местах.",
                "Сложность: O(n²) сравнений. Память: O(1)."),
            new AlgorithmOption(
                SortAlgorithm.BubbleSort,
                "Сортировка пузырьком",
                "Квадратичная группа · BubbleSort",
                "Соседние элементы сравниваются и меняются местами, если стоят неправильно. Большие числа постепенно «всплывают» вправо. Здесь включены два улучшения: ранний выход и запоминание последнего обмена.",
                "Сложность: O(n²), в лучшем случае O(n). Память: O(1)."),
            new AlgorithmOption(
                SortAlgorithm.HeapSort,
                "Пирамидальная сортировка",
                "Усовершенствованная группа · HeapSort",
                "Массив представляется в виде пирамиды. Сначала строится max-пирамида, затем максимум из корня переносится в конец, а пирамида восстанавливается для оставшейся части.",
                "Сложность: O(n log n) в любом случае. Память: O(1)."),
            new AlgorithmOption(
                SortAlgorithm.QuickSort,
                "Быстрая сортировка Хоара",
                "Усовершенствованная группа · QuickSort",
                "Выбирается опорный элемент. Все элементы меньше него отправляются влево, больше — вправо. Затем та же операция повторяется для левой и правой частей. Используется схема Ломуто.",
                "Сложность: в среднем O(n log n), в худшем O(n²). Память: O(log n) под стек."),
        ];

        _selectedAlgorithm = Algorithms[0];
        RandomSizes = [8, 12, 16, 20, 25, 30, 40, 50, 60];
        BuildFromText(ArrayText);
    }

    public IReadOnlyList<AlgorithmOption> Algorithms { get; }

    public IReadOnlyList<int> RandomSizes { get; }

    public ObservableCollection<BarViewModel> Bars { get; } = [];

    public ObservableCollection<LogEntryViewModel> Log { get; } = [];

    public string DelayLabel => DelayMs <= 0 ? "без задержки" : $"{DelayMs:0} мс";

    public string AlgorithmDescription => SelectedAlgorithm.Description;

    public string AlgorithmComplexity => SelectedAlgorithm.Complexity;

    public string AlgorithmGroup => SelectedAlgorithm.Group;

    public bool CanStart => !IsRunning;

    public bool CanStop => IsRunning;

    public bool CanPause => IsRunning;

    public string PauseText => IsPaused ? "Продолжить" : "Пауза";

    public void Stop() => _cancellation?.Cancel();

    partial void OnDelayMsChanged(double value) => OnPropertyChanged(nameof(DelayLabel));

    partial void OnSelectedAlgorithmChanged(AlgorithmOption value)
    {
        OnPropertyChanged(nameof(AlgorithmDescription));
        OnPropertyChanged(nameof(AlgorithmComplexity));
        OnPropertyChanged(nameof(AlgorithmGroup));
    }

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(CanPause));
    }

    partial void OnIsPausedChanged(bool value) => OnPropertyChanged(nameof(PauseText));

    [RelayCommand]
    private void GenerateRandom()
    {
        if (IsRunning)
        {
            return;
        }

        var size = Math.Clamp(SelectedRandomSize, MinArraySize, MaxArraySize);
        var random = new Random();
        var values = new int[size];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = random.Next(1, 100);
        }

        ArrayText = string.Join(", ", values);
        ResetView(values, $"Сгенерирован случайный массив из {size} чисел. Нажмите «Старт».");
    }

    [RelayCommand]
    private void Reset()
    {
        if (IsRunning)
        {
            return;
        }

        if (TryParseArray(ArrayText, out var values, out var error))
        {
            ResetView(values, "Массив готов. Нажмите «Старт».");
        }
        else
        {
            BuildFromText(ArrayText);
            Status = error;
            Explanation = error;
            AppendLog(error, AppPalette.Bad);
        }
    }

    [RelayCommand]
    private void TogglePause()
    {
        if (!IsRunning)
        {
            return;
        }

        IsPaused = !IsPaused;
        Status = IsPaused ? "Пауза. Нажмите «Продолжить»." : "Продолжаем сортировку…";
    }

    [RelayCommand]
    private void StopSorting() => Stop();

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning)
        {
            return;
        }

        if (!TryParseArray(ArrayText, out var values, out var error))
        {
            Status = error;
            Explanation = error;
            AppendLog(error, AppPalette.Bad);
            return;
        }

        BuildBars(values);
        Log.Clear();
        _stepNumber = 0;
        Comparisons = 0;
        Swaps = 0;
        Steps = 0;
        Status = "Сортировка выполняется…";
        Explanation = $"Запущен алгоритм «{SelectedAlgorithm.Title}».";
        AppendLog(
            $"Старт: {SelectedAlgorithm.Title}; элементов — {values.Length}; задержка — {DelayLabel}.",
            AppPalette.Accent);

        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        IsRunning = true;
        IsPaused = false;

        try
        {
            var working = (int[])values.Clone();
            using var enumerator = SortRunner.Run(SelectedAlgorithm.Value, working).GetEnumerator();
            var eventsSinceYield = 0;

            while (enumerator.MoveNext())
            {
                cancellation.Token.ThrowIfCancellationRequested();

                while (IsPaused && !cancellation.Token.IsCancellationRequested)
                {
                    await Task.Delay(60, cancellation.Token);
                }

                var sortEvent = enumerator.Current;
                ApplyEvent(sortEvent);
                _stepNumber++;
                Steps++;

                if (sortEvent.Kind == SortEventKind.Compare)
                {
                    Comparisons++;
                }
                else if (sortEvent.Kind == SortEventKind.Swap)
                {
                    Swaps++;
                }

                if (!string.IsNullOrWhiteSpace(sortEvent.LogText))
                {
                    AppendLog(
                        $"#{_stepNumber:D3}  {sortEvent.LogText}",
                        sortEvent.Kind == SortEventKind.Swap ? AppPalette.Swap : AppPalette.Warn);
                }

                if (!string.IsNullOrWhiteSpace(sortEvent.Explanation))
                {
                    Explanation = sortEvent.Explanation;
                }

                var delay = (int)Math.Round(DelayMs);
                if (delay > 0)
                {
                    await Task.Delay(delay, cancellation.Token);
                }
                else if (++eventsSinceYield >= 25)
                {
                    eventsSinceYield = 0;
                    await Task.Yield();
                }
            }

            MarkAllSorted();
            Status = $"Готово! Сравнений: {Comparisons}, перестановок: {Swaps}.";
            Explanation = $"Массив отсортирован. Алгоритм «{SelectedAlgorithm.Title}» сделал {Comparisons} сравнений и {Swaps} перестановок.";
            AppendLog(
                $"Финиш: массив отсортирован за {Comparisons} сравнений и {Swaps} перестановок.",
                AppPalette.Good);
        }
        catch (OperationCanceledException)
        {
            Status = "Сортировка остановлена пользователем.";
            Explanation = "Вы остановили сортировку. Можно изменить массив или запустить алгоритм заново.";
            AppendLog("Остановлено пользователем.", AppPalette.Muted);
        }
        finally
        {
            IsRunning = false;
            IsPaused = false;
            _cancellation = null;
        }
    }

    private void ResetView(int[] values, string status)
    {
        BuildBars(values);
        Log.Clear();
        _stepNumber = 0;
        Comparisons = 0;
        Swaps = 0;
        Steps = 0;
        Status = status;
        Explanation = "Нажмите «Старт», чтобы увидеть сортировку по шагам.";
    }

    private void BuildFromText(string text)
    {
        if (TryParseArray(text, out var values, out _))
        {
            BuildBars(values);
        }
        else
        {
            BuildBars([42, 8, 17, 3, 91, 25, 60, 14, 73, 31, 5, 88, 47, 12, 66, 29]);
        }
    }

    private void BuildBars(int[] values)
    {
        ArrayLength = values.Length;
        _sortedIndices.Clear();
        _pivotIndex = -1;
        var max = values.Length == 0 ? 1 : values.Max();
        var showLabels = values.Length <= 32;

        Bars.Clear();
        foreach (var value in values)
        {
            Bars.Add(new BarViewModel(value, max, showLabels));
        }
    }

    private void ApplyEvent(SortEvent sortEvent)
    {
        var max = sortEvent.Values.Length == 0 ? 1 : sortEvent.Values.Max();
        for (var index = 0; index < Bars.Count && index < sortEvent.Values.Length; index++)
        {
            Bars[index].Update(sortEvent.Values[index], max);
            Bars[index].Fill = AppPalette.Bar;
        }

        switch (sortEvent.Kind)
        {
            case SortEventKind.Compare:
                SetFill(sortEvent.IndexA, AppPalette.Compare);
                SetFill(sortEvent.IndexB, AppPalette.Compare);
                break;
            case SortEventKind.Swap:
                SetFill(sortEvent.IndexA, AppPalette.Swap);
                SetFill(sortEvent.IndexB, AppPalette.Swap);
                break;
            case SortEventKind.Pivot:
                _pivotIndex = sortEvent.IndexA;
                break;
            case SortEventKind.MarkSorted:
                _sortedIndices.Add(sortEvent.SortedIndex);
                _pivotIndex = -1;
                break;
        }

        if (_pivotIndex >= 0 && _pivotIndex < Bars.Count)
        {
            Bars[_pivotIndex].Fill = AppPalette.Pivot;
        }

        foreach (var index in _sortedIndices)
        {
            SetFill(index, AppPalette.Sorted);
        }
    }

    private void MarkAllSorted()
    {
        for (var index = 0; index < Bars.Count; index++)
        {
            Bars[index].Fill = AppPalette.Sorted;
        }
    }

    private void SetFill(int index, Avalonia.Media.IBrush brush)
    {
        if (index >= 0 && index < Bars.Count)
        {
            Bars[index].Fill = brush;
        }
    }

    private void AppendLog(string text, Avalonia.Media.IBrush brush)
        => Log.Add(new LogEntryViewModel(text, brush));

    private static bool TryParseArray(string text, out int[] values, out string error)
    {
        values = [];
        error = string.Empty;

        var parts = text.Split(
            [',', ';', ' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < MinArraySize)
        {
            error = $"Введите хотя бы {MinArraySize} числа через запятую или пробел.";
            return false;
        }

        if (parts.Length > MaxArraySize)
        {
            error = $"Для наглядной визуализации используйте не больше {MaxArraySize} чисел.";
            return false;
        }

        var result = new int[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], out var value) || value is < 1 or > 999)
            {
                error = $"«{parts[index]}» — не число от 1 до 999.";
                return false;
            }

            result[index] = value;
        }

        values = result;
        return true;
    }
}
