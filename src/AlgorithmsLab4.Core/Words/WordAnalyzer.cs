using System.Diagnostics;

namespace AlgorithmsLab4.Core.Words;

/// <summary>
/// Результат обработки текста: исходные слова, два отсортированных массива,
/// статистика и частоты слов.
/// </summary>
public sealed record WordAnalysis(
    string[] Words,
    string[] QuickSorted,
    string[] RadixSorted,
    long QuickComparisons,
    long QuickSwaps,
    long RadixOperations,
    double QuickMilliseconds,
    double RadixMilliseconds,
    IReadOnlyList<WordCount> Counts)
{
    public bool ResultsMatch => QuickSorted.AsSpan().SequenceEqual(RadixSorted);

    public int UniqueWords => Counts.Count;
}

/// <summary>
/// Связывает токенизатор, два алгоритма сортировки и подсчёт частот.
/// </summary>
public static class WordAnalyzer
{
    /// <summary>
    /// Разбивает текст на слова, сортирует его быстрой и поразрядной
    /// сортировкой, проверяет совпадение результатов и считает частоты.
    /// </summary>
    public static WordAnalysis Analyze(string? text)
    {
        var words = WordTokenizer.Tokenize(text);
        var quickSorted = (string[])words.Clone();
        var radixSorted = (string[])words.Clone();

        var quickWatch = Stopwatch.StartNew();
        var quickMetrics = QuickSorter.Sort(quickSorted);
        quickWatch.Stop();

        var radixWatch = Stopwatch.StartNew();
        var radixOperations = RadixSorter.Sort(radixSorted);
        radixWatch.Stop();

        var counts = CountSorted(quickSorted);
        return new WordAnalysis(
            words,
            quickSorted,
            radixSorted,
            quickMetrics.Comparisons,
            quickMetrics.Swaps,
            radixOperations,
            quickWatch.Elapsed.TotalMilliseconds,
            radixWatch.Elapsed.TotalMilliseconds,
            counts);
    }

    /// <summary>
    /// Проходит по отсортированному массиву и подсчитывает повторы.
    /// Одинаковые слова стоят рядом, поэтому достаточно одного прохода.
    /// </summary>
    public static IReadOnlyList<WordCount> CountSorted(IReadOnlyList<string> sortedWords)
    {
        ArgumentNullException.ThrowIfNull(sortedWords);
        if (sortedWords.Count == 0)
        {
            return [];
        }

        var counts = new List<WordCount>();
        var current = sortedWords[0];
        var count = 1;

        for (var index = 1; index < sortedWords.Count; index++)
        {
            if (string.Equals(sortedWords[index], current, StringComparison.Ordinal))
            {
                count++;
                continue;
            }

            counts.Add(new WordCount(current, count));
            current = sortedWords[index];
            count = 1;
        }

        counts.Add(new WordCount(current, count));
        return counts;
    }
}
