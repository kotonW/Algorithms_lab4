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
    private const int MaxArraySize = 100;
    private const int MinValue = 1;
    private const int MaxValue = 999;

    private readonly HashSet<int> _sortedIndices = [];
    private CancellationTokenSource? _playCts;
    private SortEvent[]? _events;
    private int[] _initialValues = [];
    private int[] _comparePrefix = [];
    private int[] _swapPrefix = [];
    private string _startExplanation = string.Empty;
    private bool _finishLogged;

    [ObservableProperty]
    private AlgorithmOption _selectedAlgorithm;

    [ObservableProperty]
    private string _arrayText = "42, 8, 17, 3, 91, 25, 60, 14, 73, 31, 5, 88, 47, 12, 66, 29";

    [ObservableProperty]
    private decimal? _randomSize = 20;

    [ObservableProperty]
    private decimal? _randomMin = 1;

    [ObservableProperty]
    private decimal? _randomMax = 99;

    [ObservableProperty]
    private double _delayMs = 140;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _status = "Выберите алгоритм и нажмите «Старт» или «Пошагово».";

    [ObservableProperty]
    private string _explanation = "«Старт» — алгоритм сам идёт по шагам с выбранной задержкой. «Пошагово» — шаги переключаете вы кнопками «Вперёд» и «Назад».";

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
        BuildFromText(ArrayText);
    }

    public IReadOnlyList<AlgorithmOption> Algorithms { get; }

    public ObservableCollection<BarViewModel> Bars { get; } = [];

    public ObservableCollection<LogEntryViewModel> Log { get; } = [];

    public string DelayLabel => DelayMs <= 0 ? "без задержки" : $"{DelayMs:0} мс";

    public string AlgorithmDescription => SelectedAlgorithm.Description;

    public string AlgorithmComplexity => SelectedAlgorithm.Complexity;

    public string AlgorithmGroup => SelectedAlgorithm.Group;

    public bool HasSession => _events is not null;

    public bool IsFinished => _events is not null && Steps >= _events.Length;

    /// <summary>Сессия начата, но алгоритм ещё не дошёл до конца.</summary>
    public bool IsRunning => _events is not null && Steps < _events.Length;

    public bool CanStart => !IsPlaying;

    public bool CanStop => IsRunning;

    public bool CanPause => IsRunning;

    public bool CanBack => _events is not null && Steps > 0;

    public bool CanForward => _events is not null && Steps < _events.Length;

    public string PauseText => IsPlaying ? "Пауза" : "Продолжить";

    public string ProgressText => _events is null ? "Шаг 0" : $"Шаг {Steps} из {_events.Length}";

    public void Stop()
    {
        _playCts?.Cancel();
    }

    partial void OnDelayMsChanged(double value) => OnPropertyChanged(nameof(DelayLabel));

    partial void OnSelectedAlgorithmChanged(AlgorithmOption value)
    {
        OnPropertyChanged(nameof(AlgorithmDescription));
        OnPropertyChanged(nameof(AlgorithmComplexity));
        OnPropertyChanged(nameof(AlgorithmGroup));
    }

    partial void OnIsPlayingChanged(bool value) => NotifyState();

    partial void OnStepsChanged(int value) => NotifyState();

    [RelayCommand]
    private void GenerateRandom()
    {
        EndSession();

        var size = Math.Clamp((int)(RandomSize ?? 20), MinArraySize, MaxArraySize);
        var min = Math.Clamp((int)(RandomMin ?? MinValue), MinValue, MaxValue);
        var max = Math.Clamp((int)(RandomMax ?? 99), MinValue, MaxValue);
        if (min > max)
        {
            (min, max) = (max, min);
        }

        RandomSize = size;
        RandomMin = min;
        RandomMax = max;

        var random = new Random();
        var values = new int[size];
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = random.Next(min, max + 1);
        }

        ArrayText = string.Join(", ", values);
        ResetView(values, $"Сгенерирован массив: {size} чисел от {min} до {max}. Нажмите «Старт» или «Пошагово».");
    }

    [RelayCommand]
    private void Reset()
    {
        EndSession();

        if (TryParseArray(ArrayText, out var values, out var error))
        {
            ResetView(values, "Массив готов. Нажмите «Старт» или «Пошагово».");
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
    private async Task TogglePauseAsync()
    {
        if (_events is null || Steps >= _events.Length)
        {
            return;
        }

        if (IsPlaying)
        {
            PausePlayback();
        }
        else
        {
            await PlayAsync();
        }
    }

    [RelayCommand]
    private void StopSorting()
    {
        if (!IsRunning)
        {
            return;
        }

        EndSession();
        Status = "Сортировка остановлена пользователем.";
        Explanation = "Вы остановили сортировку. Можно изменить массив или запустить алгоритм заново.";
        AppendLog("Остановлено пользователем.", AppPalette.Muted);
    }

    [RelayCommand]
    private Task StartAsync() => BeginSessionAsync(autoPlay: true);

    [RelayCommand]
    private Task StartManualAsync() => BeginSessionAsync(autoPlay: false);

    [RelayCommand]
    private void StepForward()
    {
        if (_events is null || Steps >= _events.Length)
        {
            return;
        }

        PausePlayback();
        StepForwardCore();
    }

    [RelayCommand]
    private void StepBack()
    {
        if (_events is null || Steps <= 0)
        {
            return;
        }

        PausePlayback();

        if (_finishLogged)
        {
            RemoveLastLog();
            _finishLogged = false;
        }

        if (!string.IsNullOrWhiteSpace(_events[Steps - 1].LogText))
        {
            RemoveLastLog();
        }

        Steps--;
        RenderPosition();
        UpdateStatus();
    }

    private async Task BeginSessionAsync(bool autoPlay)
    {
        if (IsPlaying)
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

        EndSession();
        BuildBars(values);
        Log.Clear();

        var events = SortRunner.Run(SelectedAlgorithm.Value, (int[])values.Clone()).ToArray();
        _comparePrefix = new int[events.Length + 1];
        _swapPrefix = new int[events.Length + 1];
        for (var index = 0; index < events.Length; index++)
        {
            _comparePrefix[index + 1] = _comparePrefix[index] + (events[index].Kind == SortEventKind.Compare ? 1 : 0);
            _swapPrefix[index + 1] = _swapPrefix[index] + (events[index].Kind == SortEventKind.Swap ? 1 : 0);
        }

        _initialValues = values;
        _events = events;
        _finishLogged = false;
        Comparisons = 0;
        Swaps = 0;
        Steps = 0;

        _startExplanation = autoPlay
            ? $"Запущен алгоритм «{SelectedAlgorithm.Title}»."
            : $"Алгоритм «{SelectedAlgorithm.Title}» готов. Нажимайте «Вперёд», чтобы увидеть следующий шаг, и «Назад», чтобы вернуться.";
        Explanation = _startExplanation;
        AppendLog(
            autoPlay
                ? $"Старт: {SelectedAlgorithm.Title}; элементов — {values.Length}; задержка — {DelayLabel}."
                : $"Пошаговый режим: {SelectedAlgorithm.Title}; элементов — {values.Length}; шагов — {events.Length}.",
            AppPalette.Accent);

        NotifyState();
        UpdateStatus();

        if (autoPlay)
        {
            await PlayAsync();
        }
    }

    private async Task PlayAsync()
    {
        if (_events is null || IsPlaying || Steps >= _events.Length)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _playCts = cts;
        IsPlaying = true;
        UpdateStatus();

        var eventsSinceYield = 0;
        try
        {
            while (_events is not null && Steps < _events.Length)
            {
                cts.Token.ThrowIfCancellationRequested();
                StepForwardCore();

                var delay = (int)Math.Round(DelayMs);
                if (delay > 0)
                {
                    await Task.Delay(delay, cts.Token);
                }
                else if (++eventsSinceYield >= 25)
                {
                    eventsSinceYield = 0;
                    await Task.Yield();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Пауза или остановка — это нормальный выход из цикла.
        }
        finally
        {
            if (ReferenceEquals(_playCts, cts))
            {
                _playCts = null;
                IsPlaying = false;
                UpdateStatus();
            }

            cts.Dispose();
        }
    }

    private void PausePlayback()
    {
        if (!IsPlaying)
        {
            return;
        }

        _playCts?.Cancel();
        _playCts = null;
        IsPlaying = false;
        UpdateStatus();
    }

    /// <summary>Завершает сессию: автоход выключается, список шагов сбрасывается.</summary>
    private void EndSession()
    {
        _playCts?.Cancel();
        _playCts = null;
        _events = null;
        _finishLogged = false;
        IsPlaying = false;
        NotifyState();
    }

    private void StepForwardCore()
    {
        if (_events is null || Steps >= _events.Length)
        {
            return;
        }

        var sortEvent = _events[Steps];
        Steps++;

        if (!string.IsNullOrWhiteSpace(sortEvent.LogText))
        {
            AppendLog(
                $"#{Steps:D3}  {sortEvent.LogText}",
                sortEvent.Kind == SortEventKind.Swap ? AppPalette.Swap : AppPalette.Warn);
        }

        RenderPosition();

        if (Steps >= _events.Length && !_finishLogged)
        {
            _finishLogged = true;
            AppendLog(
                $"Финиш: массив отсортирован за {Comparisons} сравнений и {Swaps} перестановок.",
                AppPalette.Good);
        }

        UpdateStatus();
    }

    /// <summary>
    /// Рисует состояние после <see cref="Steps"/> шагов. Состояние считается заново
    /// по списку событий, поэтому шаг назад работает так же, как шаг вперёд.
    /// </summary>
    private void RenderPosition()
    {
        var events = _events;
        if (events is null)
        {
            return;
        }

        var position = Math.Clamp(Steps, 0, events.Length);
        Comparisons = _comparePrefix[position];
        Swaps = _swapPrefix[position];

        var values = position == 0 ? _initialValues : events[position - 1].Values;
        var max = values.Length == 0 ? 1 : values.Max();
        for (var index = 0; index < Bars.Count && index < values.Length; index++)
        {
            Bars[index].Update(values[index], max);
            Bars[index].Fill = AppPalette.Bar;
        }

        if (position == events.Length)
        {
            MarkAllSorted();
            Explanation = $"Массив отсортирован. Алгоритм «{SelectedAlgorithm.Title}» сделал {Comparisons} сравнений и {Swaps} перестановок.";
            return;
        }

        _sortedIndices.Clear();
        var pivotIndex = -1;
        for (var index = 0; index < position; index++)
        {
            var past = events[index];
            if (past.Kind == SortEventKind.Pivot)
            {
                pivotIndex = past.IndexA;
            }
            else if (past.Kind == SortEventKind.MarkSorted)
            {
                _sortedIndices.Add(past.SortedIndex);
                pivotIndex = -1;
            }
        }

        if (position == 0)
        {
            Explanation = _startExplanation;
            return;
        }

        var last = events[position - 1];
        switch (last.Kind)
        {
            case SortEventKind.Compare:
                SetFill(last.IndexA, AppPalette.Compare);
                SetFill(last.IndexB, AppPalette.Compare);
                break;
            case SortEventKind.Swap:
                SetFill(last.IndexA, AppPalette.Swap);
                SetFill(last.IndexB, AppPalette.Swap);
                break;
        }

        if (pivotIndex >= 0 && pivotIndex < Bars.Count)
        {
            Bars[pivotIndex].Fill = AppPalette.Pivot;
        }

        foreach (var index in _sortedIndices)
        {
            SetFill(index, AppPalette.Sorted);
        }

        if (!string.IsNullOrWhiteSpace(last.Explanation))
        {
            Explanation = last.Explanation;
        }
    }

    private void UpdateStatus()
    {
        if (_events is null)
        {
            return;
        }

        if (Steps >= _events.Length)
        {
            Status = $"Готово! Сравнений: {Comparisons}, перестановок: {Swaps}. Кнопка «Назад» вернёт к любому шагу.";
        }
        else if (IsPlaying)
        {
            Status = "Сортировка выполняется…";
        }
        else
        {
            Status = $"Шаг {Steps} из {_events.Length}. Листайте «Вперёд» и «Назад» или нажмите «Продолжить».";
        }
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasSession));
        OnPropertyChanged(nameof(IsFinished));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanBack));
        OnPropertyChanged(nameof(CanForward));
        OnPropertyChanged(nameof(PauseText));
        OnPropertyChanged(nameof(ProgressText));
    }

    private void RemoveLastLog()
    {
        if (Log.Count > 0)
        {
            Log.RemoveAt(Log.Count - 1);
        }
    }

    private void ResetView(int[] values, string status)
    {
        BuildBars(values);
        Log.Clear();
        Comparisons = 0;
        Swaps = 0;
        Steps = 0;
        Status = status;
        Explanation = "Нажмите «Старт» (автоматически) или «Пошагово» (кнопками «Вперёд»/«Назад»).";
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
        var max = values.Length == 0 ? 1 : values.Max();
        var showLabels = values.Length <= 32;

        Bars.Clear();
        foreach (var value in values)
        {
            Bars.Add(new BarViewModel(value, max, showLabels));
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
            if (!int.TryParse(parts[index], out var value) || value is < MinValue or > MaxValue)
            {
                error = $"«{parts[index]}» — не число от {MinValue} до {MaxValue}.";
                return false;
            }

            result[index] = value;
        }

        values = result;
        return true;
    }
}
