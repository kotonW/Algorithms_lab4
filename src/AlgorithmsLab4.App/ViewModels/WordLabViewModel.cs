using AlgorithmsLab4.Core.Words;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace AlgorithmsLab4.App.ViewModels;

public sealed record WordCountItem(string Word, int Count, int MaxCount)
{
    public string CountText => Count.ToString(CultureInfo.CurrentCulture);
}

public sealed record BenchmarkRowItem(
    int WordCount,
    string QuickTime,
    string RadixTime,
    long QuickComparisons,
    long RadixOperations,
    int UniqueWords,
    bool ResultsMatch)
{
    public string MatchText => ResultsMatch ? "да" : "нет";

    public IBrush MatchBrush => ResultsMatch ? AppPalette.Good : AppPalette.Bad;
}

/// <summary>
/// ViewModel задания 3: разбиение текста на слова, сортировка QuickSort и
/// radix sort, подсчёт частот и таблица экспериментов для разных длин текста.
/// </summary>
public sealed partial class WordLabViewModel : ViewModelBase
{
    private static readonly int[] DefaultBenchmarkSizes = [100, 500, 1000, 2000, 5000, 10000];

    private CancellationTokenSource? _benchmarkCancellation;

    [ObservableProperty]
    private string _inputText = TextCorpus.SampleText;

    [ObservableProperty]
    private string _status = "Нажмите «Анализировать», чтобы разбить текст на слова и отсортировать их двумя способами.";

    [ObservableProperty]
    private string _sortedWordsText = string.Empty;

    [ObservableProperty]
    private string _quickTimeText = "—";

    [ObservableProperty]
    private string _radixTimeText = "—";

    [ObservableProperty]
    private string _matchText = "—";

    [ObservableProperty]
    private IBrush _matchBrush = AppPalette.Muted;

    [ObservableProperty]
    private long _quickComparisons;

    [ObservableProperty]
    private long _radixOperations;

    [ObservableProperty]
    private int _totalWords;

    [ObservableProperty]
    private int _uniqueWords;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private bool _isBenchmarking;

    [ObservableProperty]
    private double _benchmarkProgress;

    [ObservableProperty]
    private string _benchmarkStatus = "Запустите эксперименты для длин 100, 500, 1000, 2000, 5000 и 10 000 слов.";

    [ObservableProperty]
    private bool _useInputTextForBenchmarks = true;

    public WordLabViewModel()
    {
        BenchmarkSizes = DefaultBenchmarkSizes;
    }

    public IReadOnlyList<int> BenchmarkSizes { get; }

    public ObservableCollection<WordCountItem> Counts { get; } = [];

    public ObservableCollection<BenchmarkRowItem> BenchmarkRows { get; } = [];

    public bool CanAnalyze => !IsAnalyzing && !IsBenchmarking;

    public bool CanRunBenchmarks => !IsAnalyzing && !IsBenchmarking;

    public bool CanStopBenchmarks => IsBenchmarking;

    public bool CanSaveBenchmarks => BenchmarkRows.Count > 0 && !IsBenchmarking;

    public string QuickComparisonsText => $"сравнений: {QuickComparisons}";

    public string RadixOperationsText => $"операций: {RadixOperations}";

    public string BenchmarkSourceLabel
        => UseInputTextForBenchmarks
            ? "Для экспериментов используется текущий текст (слова повторяются до нужной длины)."
            : "Для экспериментов используется встроенный английский корпус.";

    public void Stop()
    {
        _benchmarkCancellation?.Cancel();
    }

    partial void OnQuickComparisonsChanged(long value)
        => OnPropertyChanged(nameof(QuickComparisonsText));

    partial void OnRadixOperationsChanged(long value)
        => OnPropertyChanged(nameof(RadixOperationsText));

    partial void OnIsAnalyzingChanged(bool value) => NotifyStateChanged();

    partial void OnIsBenchmarkingChanged(bool value) => NotifyStateChanged();

    partial void OnUseInputTextForBenchmarksChanged(bool value)
        => OnPropertyChanged(nameof(BenchmarkSourceLabel));

    [RelayCommand]
    private void LoadSample()
    {
        InputText = TextCorpus.SampleText;
        ClearResults();
        Status = "Загружен встроенный английский текст. Нажмите «Анализировать».";
    }

    [RelayCommand]
    private void Clear()
    {
        if (IsAnalyzing || IsBenchmarking)
        {
            return;
        }

        InputText = string.Empty;
        ClearResults();
        Status = "Введите текст или загрузите файл.";
    }

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        if (IsAnalyzing || IsBenchmarking)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(InputText))
        {
            Status = "Сначала введите текст или загрузите файл.";
            return;
        }

        IsAnalyzing = true;
        Status = "Разбиваем текст на слова и сортируем двумя алгоритмами…";
        try
        {
            var analysis = await Task.Run(() => WordAnalyzer.Analyze(InputText));

            TotalWords = analysis.Words.Length;
            UniqueWords = analysis.UniqueWords;
            QuickComparisons = analysis.QuickComparisons;
            RadixOperations = analysis.RadixOperations;
            QuickTimeText = FormatMilliseconds(analysis.QuickMilliseconds);
            RadixTimeText = FormatMilliseconds(analysis.RadixMilliseconds);
            MatchText = analysis.ResultsMatch ? "совпали" : "не совпали";
            MatchBrush = analysis.ResultsMatch ? AppPalette.Good : AppPalette.Bad;
            SortedWordsText = string.Join(' ', analysis.QuickSorted);

            Counts.Clear();
            var maxCount = analysis.Counts.Count == 0 ? 1 : analysis.Counts.Max(item => item.Count);
            foreach (var count in analysis.Counts)
            {
                Counts.Add(new WordCountItem(count.Word, count.Count, maxCount));
            }

            Status = TotalWords == 0
                ? "В тексте не найдено слов. Проверьте ввод."
                : $"Готово: {TotalWords} слов, {UniqueWords} уникальных. Слова отсортированы, частоты подсчитаны.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            Status = $"Не удалось обработать текст: {exception.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private async Task RunBenchmarksAsync()
    {
        if (IsAnalyzing || IsBenchmarking)
        {
            return;
        }

        BenchmarkRows.Clear();
        BenchmarkProgress = 0;
        BenchmarkStatus = "Подготовка экспериментов…";
        OnPropertyChanged(nameof(CanSaveBenchmarks));

        string[]? sourceWords = null;
        if (UseInputTextForBenchmarks)
        {
            var tokens = WordTokenizer.Tokenize(InputText);
            if (tokens.Length > 0)
            {
                sourceWords = tokens;
            }
        }

        using var cancellation = new CancellationTokenSource();
        _benchmarkCancellation = cancellation;
        IsBenchmarking = true;

        try
        {
            for (var index = 0; index < BenchmarkSizes.Count; index++)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var size = BenchmarkSizes[index];
                BenchmarkStatus = $"Эксперимент {index + 1} из {BenchmarkSizes.Count}: {size} слов…";

                var result = await Task.Run(
                    () => BenchmarkRunner.RunOne(size, sourceWords, seed: 20240501),
                    cancellation.Token);

                BenchmarkRows.Add(new BenchmarkRowItem(
                    result.WordCount,
                    FormatMilliseconds(result.QuickMilliseconds),
                    FormatMilliseconds(result.RadixMilliseconds),
                    result.QuickComparisons,
                    result.RadixOperations,
                    result.UniqueWords,
                    result.ResultsMatch));

                BenchmarkProgress = (index + 1) * 100.0 / BenchmarkSizes.Count;
                OnPropertyChanged(nameof(CanSaveBenchmarks));
            }

            BenchmarkStatus = "Эксперименты завершены. Данные можно сохранить в CSV для отчёта.";
        }
        catch (OperationCanceledException)
        {
            BenchmarkStatus = "Эксперименты остановлены пользователем.";
        }
        finally
        {
            IsBenchmarking = false;
            _benchmarkCancellation = null;
            OnPropertyChanged(nameof(CanSaveBenchmarks));
        }
    }

    [RelayCommand]
    private void StopBenchmarks() => Stop();

    [RelayCommand]
    private void SaveBenchmarks()
    {
        if (BenchmarkRows.Count == 0)
        {
            BenchmarkStatus = "Таблица экспериментов пока пуста.";
            return;
        }

        try
        {
            var directory = PrepareExperimentDirectory();
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"benchmarks_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            var builder = new StringBuilder();
            builder.AppendLine("Слов;QuickSort, мс;Radix sort, мс;Сравнения QuickSort;Операции Radix;Уникальных слов;Результаты совпали");
            foreach (var row in BenchmarkRows)
            {
                builder.Append(row.WordCount.ToString(CultureInfo.CurrentCulture)).Append(';')
                    .Append(row.QuickTime.Replace(" мс", string.Empty, StringComparison.Ordinal)).Append(';')
                    .Append(row.RadixTime.Replace(" мс", string.Empty, StringComparison.Ordinal)).Append(';')
                    .Append(row.QuickComparisons.ToString(CultureInfo.CurrentCulture)).Append(';')
                    .Append(row.RadixOperations.ToString(CultureInfo.CurrentCulture)).Append(';')
                    .Append(row.UniqueWords.ToString(CultureInfo.CurrentCulture)).Append(';')
                    .AppendLine(row.MatchText);
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            BenchmarkStatus = $"Таблица сохранена: {path}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            BenchmarkStatus = $"Не удалось сохранить таблицу: {exception.Message}";
        }
    }

    /// <summary>Загружает текстовый файл, выбранный в окне.</summary>
    public async Task LoadTextAsync(string path)
    {
        if (IsAnalyzing || IsBenchmarking)
        {
            return;
        }

        try
        {
            InputText = await File.ReadAllTextAsync(path, Encoding.UTF8);
            ClearResults();
            Status = $"Текст загружен из «{path}». Нажмите «Анализировать».";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Status = $"Не удалось прочитать файл: {exception.Message}";
        }
    }

    /// <summary>
    /// Выбирает папку для CSV с результатами экспериментов. Если папка
    /// пользователя недоступна, используется папка программы или временная.
    /// </summary>
    private static string PrepareExperimentDirectory()
    {
        string[] candidates =
        [
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AlgorithmsLab4",
                "experiments"),
            Path.Combine(AppContext.BaseDirectory, "experiments"),
            Path.Combine(Path.GetTempPath(), "AlgorithmsLab4", "experiments"),
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

    private void ClearResults()
    {
        SortedWordsText = string.Empty;
        Counts.Clear();
        TotalWords = 0;
        UniqueWords = 0;
        QuickComparisons = 0;
        RadixOperations = 0;
        QuickTimeText = "—";
        RadixTimeText = "—";
        MatchText = "—";
        MatchBrush = AppPalette.Muted;
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CanAnalyze));
        OnPropertyChanged(nameof(CanRunBenchmarks));
        OnPropertyChanged(nameof(CanStopBenchmarks));
        OnPropertyChanged(nameof(CanSaveBenchmarks));
    }

    private static string FormatMilliseconds(double milliseconds)
        => milliseconds < 0.001
            ? "< 0,001 мс"
            : milliseconds.ToString("0.000", CultureInfo.CurrentCulture) + " мс";
}
