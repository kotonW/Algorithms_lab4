using AlgorithmsLab4.Core;
using AlgorithmsLab4.Core.ExternalSort;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlgorithmsLab4.App.ViewModels;

public sealed record ColumnOption(int Index, string Name)
{
    public override string ToString() => Name;
}

public sealed record ExternalMethodOption(
    ExternalSortMethod Value,
    string Title,
    string Description);

/// <summary>
/// ViewModel задания 2: внешняя сортировка CSV-таблицы с выбором метода,
/// ключевой колонки, фильтра и задержкой между шагами.
/// </summary>
public sealed partial class ExternalSortViewModel : ViewModelBase
{
    private static readonly ColumnOption NoFilter = new(-1, "Без фильтра");

    private string _samplePath;
    private readonly string _tempRoot;
    private CsvTable? _table;
    private CancellationTokenSource? _cancellation;

    [ObservableProperty]
    private string _inputPath = string.Empty;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string _inputPreview = "Загрузите CSV-файл или нажмите «Пример из задания».";

    [ObservableProperty]
    private string _resultPreview = "Результат появится здесь после сортировки.";

    [ObservableProperty]
    private string _status = "Загрузите таблицу, чтобы начать.";

    [ObservableProperty]
    private string _explanation = "Таблица читается из файла, сортируется методом слияния и записывается в новый CSV-файл.";

    [ObservableProperty]
    private ColumnOption? _selectedKeyColumn;

    [ObservableProperty]
    private ColumnOption _selectedFilterColumn = NoFilter;

    [ObservableProperty]
    private string _selectedFilterValue = string.Empty;

    [ObservableProperty]
    private ExternalMethodOption _selectedMethod;

    [ObservableProperty]
    private int _selectedRunSize = 8;

    [ObservableProperty]
    private int _selectedFanIn = 3;

    [ObservableProperty]
    private bool _descending;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private double _delayMs = 35;

    [ObservableProperty]
    private long _comparisons;

    [ObservableProperty]
    private long _moves;

    [ObservableProperty]
    private int _runsCreated;

    [ObservableProperty]
    private int _mergePasses;

    [ObservableProperty]
    private int _recordCount;

    [ObservableProperty]
    private int _filteredOut;

    [ObservableProperty]
    private int _sourceRowCount;

    public ExternalSortViewModel()
    {
        Methods =
        [
            new ExternalMethodOption(
                ExternalSortMethod.DirectMerge,
                "Прямое слияние",
                "Входные записи разбиваются на блоки фиксированного размера, каждый блок сортируется в памяти, затем блоки попарно сливаются."),
            new ExternalMethodOption(
                ExternalSortMethod.NaturalMerge,
                "Естественное слияние",
                "Сначала из входного файла выделяются уже упорядоченные серии, затем они сливаются между собой. Если данные почти отсортированы, серий будет мало."),
            new ExternalMethodOption(
                ExternalSortMethod.MultiwayMerge,
                "Многопутевое слияние",
                "За один проход сливаются сразу несколько серий (например, четыре). Это уменьшает число проходов по файлам."),
        ];

        _selectedMethod = Methods[0];
        RunSizes = [2, 3, 4, 5, 8, 10, 16, 32];
        FanIns = [2, 3, 4, 5, 6];

        _samplePath = PrepareSamplePath();
        _tempRoot = PrepareTempRoot();

        try
        {
            LoadFile(_samplePath);
            Status = "Пример из задания уже загружен. Выберите метод и нажмите «Старт».";
            Explanation = "Таблица стран загружена. Ключ сортировки — численность населения.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Status = "Не удалось подготовить пример.";
            Explanation = exception.Message;
        }
    }

    /// <summary>
    /// Готовит CSV-пример. Сначала пробуем папку пользователя, затем папку
    /// программы: это нужно, чтобы демонстрация работала и в ограниченной среде.
    /// </summary>
    private static string PrepareSamplePath()
    {
        string[] candidates =
        [
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AlgorithmsLab4",
                "samples",
                "countries.csv"),
            Path.Combine(AppContext.BaseDirectory, "samples", "countries.csv"),
            Path.Combine(Path.GetTempPath(), "AlgorithmsLab4", "samples", "countries.csv"),
        ];

        foreach (var candidate in candidates)
        {
            try
            {
                SampleData.SaveCountriesSample(candidate);
                return candidate;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Пробуем следующий вариант.
            }
        }

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    /// <summary>
    /// Выбирает каталог для временных серий, в который действительно можно писать.
    /// </summary>
    private static string PrepareTempRoot()
    {
        string[] candidates =
        [
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AlgorithmsLab4",
                "temp"),
            Path.Combine(AppContext.BaseDirectory, "temp"),
            Path.Combine(Path.GetTempPath(), "AlgorithmsLab4", "temp"),
        ];

        foreach (var candidate in candidates)
        {
            try
            {
                Directory.CreateDirectory(candidate);
                var probe = Path.Combine(candidate, $"probe_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
                return candidate;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Пробуем следующий вариант.
            }
        }

        return candidates[0];
    }

    public IReadOnlyList<ExternalMethodOption> Methods { get; }

    public IReadOnlyList<int> RunSizes { get; }

    public IReadOnlyList<int> FanIns { get; }

    public ObservableCollection<ColumnOption> Columns { get; } = [];

    public ObservableCollection<ColumnOption> FilterColumns { get; } = [];

    public ObservableCollection<string> FilterValues { get; } = [];

    public ObservableCollection<LogEntryViewModel> Log { get; } = [];

    public string DelayLabel => DelayMs <= 0 ? "без задержки" : $"{DelayMs:0} мс";

    public string MethodDescription => SelectedMethod.Description;

    public bool HasFilter => SelectedFilterColumn.Index >= 0;

    public bool IsMultiway => SelectedMethod.Value == ExternalSortMethod.MultiwayMerge;

    public bool CanStart => !IsRunning && !IsLoading && _table != null;

    public bool CanStop => IsRunning;

    public bool CanPause => IsRunning;

    public bool CanInteract => !IsRunning && !IsLoading;

    public string PauseText => IsPaused ? "Продолжить" : "Пауза";

    public string DirectionLabel => Descending ? "по убыванию" : "по возрастанию";

    public string DirectionHint
        => $"Направление: {DirectionLabel}. Данные читаются и пишутся потоково.";

    public string FileInfo
        => _table == null
            ? "Файл не загружен"
            : $"{SourceRowCount} записей · {_table.ColumnCount} колонок · разделитель «{_table.Delimiter}»";

    public void Stop() => _cancellation?.Cancel();

    partial void OnDelayMsChanged(double value) => OnPropertyChanged(nameof(DelayLabel));

    partial void OnDescendingChanged(bool value)
    {
        OnPropertyChanged(nameof(DirectionLabel));
        OnPropertyChanged(nameof(DirectionHint));
    }

    partial void OnSelectedMethodChanged(ExternalMethodOption value)
    {
        OnPropertyChanged(nameof(MethodDescription));
        OnPropertyChanged(nameof(IsMultiway));
    }

    partial void OnSelectedKeyColumnChanged(ColumnOption? value) => OnPropertyChanged(nameof(CanStart));

    partial void OnSelectedFilterColumnChanged(ColumnOption value)
    {
        OnPropertyChanged(nameof(HasFilter));
        RefreshFilterValues();
    }

    partial void OnIsRunningChanged(bool value)
        => NotifyStateChanged();

    partial void OnIsLoadingChanged(bool value)
        => NotifyStateChanged();

    partial void OnIsPausedChanged(bool value) => OnPropertyChanged(nameof(PauseText));

    [RelayCommand]
    private async Task LoadExampleAsync()
    {
        if (IsRunning)
        {
            return;
        }

        try
        {
            _samplePath = PrepareSamplePath();
            await LoadFileAsync(_samplePath);
            ApplyExampleScenario();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Status = "Не удалось загрузить пример.";
            Explanation = exception.Message;
            AppendLog(exception.Message, AppPalette.Bad);
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
        if (IsRunning || _table == null)
        {
            return;
        }

        if (SelectedKeyColumn is null)
        {
            Status = "Выберите колонку-ключ.";
            return;
        }

        if (HasFilter && string.IsNullOrWhiteSpace(SelectedFilterValue))
        {
            Status = "Выберите значение фильтра или отключите фильтр.";
            return;
        }

        if (string.Equals(Path.GetFullPath(InputPath), Path.GetFullPath(OutputPath), StringComparison.OrdinalIgnoreCase))
        {
            OutputPath = Path.Combine(
                Path.GetDirectoryName(InputPath) ?? ".",
                Path.GetFileNameWithoutExtension(InputPath) + "_sorted.csv");
        }

        var options = new ExternalSortOptions
        {
            KeyColumnIndex = SelectedKeyColumn.Index,
            KeyColumnName = SelectedKeyColumn.Name,
            Descending = Descending,
            Method = SelectedMethod.Value,
            RunSize = SelectedRunSize,
            FanIn = SelectedFanIn,
            FilterColumnIndex = HasFilter ? SelectedFilterColumn.Index : -1,
            FilterValue = HasFilter ? SelectedFilterValue : null,
            TempDirectoryRoot = _tempRoot,
        };

        Log.Clear();
        ResetStats();
        ResultPreview = "Сортировка выполняется…";
        Explanation = $"Метод «{SelectedMethod.Title}». Ключ: {SelectedKeyColumn.Name}, {DirectionLabel}.";
        Status = "Сортировка выполняется…";
        AppendLog(
            $"Старт: {SelectedMethod.Title}; ключ «{SelectedKeyColumn.Name}»; {DirectionLabel}; блок {SelectedRunSize}.",
            AppPalette.Accent);

        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        IsRunning = true;
        IsPaused = false;

        try
        {
            var eventsSinceYield = 0;
            ExternalSortResult? lastResult = null;

            foreach (var step in ExternalSorter.Sort(InputPath, OutputPath, options))
            {
                cancellation.Token.ThrowIfCancellationRequested();

                while (IsPaused && !cancellation.Token.IsCancellationRequested)
                {
                    await Task.Delay(60, cancellation.Token);
                }

                Comparisons = step.Comparisons;
                Moves = step.Moves;
                RunsCreated = step.RunsCreated;
                MergePasses = step.MergePasses;
                Explanation = step.Title;

                if (!string.IsNullOrWhiteSpace(step.LogText))
                {
                    AppendLog(step.LogText, BrushFor(step.Kind));
                }

                if (step.Result is { } result)
                {
                    lastResult = result;
                }

                var delay = (int)Math.Round(DelayMs);
                if (delay > 0)
                {
                    await Task.Delay(delay, cancellation.Token);
                }
                else if (++eventsSinceYield >= 20)
                {
                    eventsSinceYield = 0;
                    await Task.Yield();
                }
            }

            if (lastResult is not null)
            {
                RecordCount = lastResult.RecordCount;
                FilteredOut = lastResult.FilteredOut;
                Comparisons = lastResult.Comparisons;
                Moves = lastResult.Moves;
                RunsCreated = lastResult.RunsCreated;
                MergePasses = lastResult.MergePasses;

                var outputTable = await Task.Run(() => CsvTable.Load(OutputPath), cancellation.Token);
                ResultPreview = outputTable.ToPreviewText(14);
                Status = $"Готово! Записей в результате: {RecordCount}.";
                Explanation = $"Файл сохранён: {OutputPath}";
                AppendLog($"Готово. Результат: {OutputPath}", AppPalette.Good);
            }
        }
        catch (OperationCanceledException)
        {
            Status = "Сортировка остановлена пользователем.";
            Explanation = "Временные файлы удалены, итоговый файл не изменялся.";
            AppendLog("Остановлено пользователем.", AppPalette.Muted);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
        {
            Status = "Ошибка внешней сортировки.";
            Explanation = exception.Message;
            AppendLog(exception.Message, AppPalette.Bad);
        }
        finally
        {
            IsRunning = false;
            IsPaused = false;
            _cancellation = null;
        }
    }

    /// <summary>
    /// Загружает произвольный CSV-файл. Вызывается из окна после выбора файла.
    /// </summary>
    public async Task LoadFileAsync(string path)
    {
        if (IsRunning)
        {
            return;
        }

        IsLoading = true;
        Status = "Читаем таблицу…";
        try
        {
            var table = await Task.Run(() => CsvTable.Load(path));
            ApplyTable(path, table);
            Status = $"Таблица загружена: {table.Rows.Count} записей.";
            Explanation = "Выберите ключ сортировки, метод и при необходимости фильтр.";
            AppendLog($"Загружен файл «{path}»: {table.Rows.Count} записей, {table.ColumnCount} колонок.", AppPalette.Accent);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Status = "Не удалось прочитать CSV-файл.";
            Explanation = exception.Message;
            AppendLog(exception.Message, AppPalette.Bad);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadFile(string path) => ApplyTable(path, CsvTable.Load(path));

    private void ApplyTable(string path, CsvTable table)
    {
        _table = table;
        InputPath = path;
        SourceRowCount = table.Rows.Count;
        OutputPath = Path.Combine(
            Path.GetDirectoryName(path) ?? ".",
            Path.GetFileNameWithoutExtension(path) + "_sorted.csv");
        InputPreview = table.ToPreviewText(14);
        ResultPreview = "Результат появится здесь после сортировки.";
        ResetStats();
        Log.Clear();

        Columns.Clear();
        for (var index = 0; index < table.Headers.Length; index++)
        {
            Columns.Add(new ColumnOption(index, table.Headers[index]));
        }

        FilterColumns.Clear();
        FilterColumns.Add(NoFilter);
        foreach (var column in Columns)
        {
            FilterColumns.Add(column);
        }

        SelectedKeyColumn = Columns.FirstOrDefault(column => column.Name.Contains("Население", StringComparison.OrdinalIgnoreCase))
            ?? Columns.LastOrDefault();
        SelectedFilterColumn = NoFilter;
        SelectedFilterValue = string.Empty;
        OnPropertyChanged(nameof(FileInfo));
        OnPropertyChanged(nameof(CanStart));
    }

    private void ApplyExampleScenario()
    {
        var population = Columns.FirstOrDefault(column => column.Name.Contains("Население", StringComparison.OrdinalIgnoreCase));
        if (population is not null)
        {
            SelectedKeyColumn = population;
        }

        SelectedMethod = Methods.First(method => method.Value == ExternalSortMethod.DirectMerge);
        SelectedRunSize = 5;
        Descending = false;

        var continent = FilterColumns.FirstOrDefault(column => column.Name == "Континент");
        if (continent is not null)
        {
            SelectedFilterColumn = continent;
            var europe = FilterValues.FirstOrDefault(value => value == "Европа");
            if (europe is not null)
            {
                SelectedFilterValue = europe;
            }
        }

        Status = "Пример из задания: страны Европы по возрастанию населения.";
        Explanation = "Можно нажать «Старт» — программа покажет блоки, сравнения и слияние файлов.";
    }

    private void RefreshFilterValues()
    {
        FilterValues.Clear();
        if (_table is null || SelectedFilterColumn.Index < 0 || SelectedFilterColumn.Index >= _table.ColumnCount)
        {
            SelectedFilterValue = string.Empty;
            return;
        }

        var comparer = StringComparer.CurrentCultureIgnoreCase;
        var values = _table.Rows
            .Select(row => row[SelectedFilterColumn.Index].Trim())
            .Where(value => value.Length > 0)
            .Distinct(comparer)
            .OrderBy(value => value, comparer)
            .ToArray();

        foreach (var value in values)
        {
            FilterValues.Add(value);
        }

        SelectedFilterValue = FilterValues.FirstOrDefault(value => value == "Европа")
            ?? FilterValues.FirstOrDefault()
            ?? string.Empty;
    }

    private void ResetStats()
    {
        Comparisons = 0;
        Moves = 0;
        RunsCreated = 0;
        MergePasses = 0;
        RecordCount = 0;
        FilteredOut = 0;
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanInteract));
    }

    private void AppendLog(string text, Avalonia.Media.IBrush brush)
        => Log.Add(new LogEntryViewModel(text, brush));

    private static Avalonia.Media.IBrush BrushFor(ExternalSortStepKind kind)
        => kind switch
        {
            ExternalSortStepKind.Comparison => AppPalette.Warn,
            ExternalSortStepKind.Move => AppPalette.Accent,
            ExternalSortStepKind.RunCreated => AppPalette.Pivot,
            ExternalSortStepKind.MergePass => AppPalette.Pivot,
            ExternalSortStepKind.Completed => AppPalette.Good,
            _ => AppPalette.Muted,
        };
}
