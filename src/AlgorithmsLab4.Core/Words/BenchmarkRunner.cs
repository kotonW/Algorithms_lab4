using System.Diagnostics;

namespace AlgorithmsLab4.Core.Words;

/// <summary>Строка таблицы экспериментов из задания 3.</summary>
public sealed record BenchmarkResult(
    int WordCount,
    double QuickMilliseconds,
    double RadixMilliseconds,
    long QuickComparisons,
    long RadixOperations,
    int UniqueWords,
    bool ResultsMatch);

/// <summary>
/// Замеряет время быстрой и поразрядной сортировки на одинаковых наборах слов.
/// Для устойчивости каждое измерение выполняется несколько раз, а в таблицу
/// попадает лучшее время.
/// </summary>
public static class BenchmarkRunner
{
    public const int DefaultRepetitions = 3;

    public static BenchmarkResult RunOne(
        int wordCount,
        IReadOnlyList<string>? sourceWords = null,
        int seed = 20240501,
        int repetitions = DefaultRepetitions)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(wordCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(repetitions);

        var words = TextCorpus.BuildWordList(wordCount, sourceWords, seed);
        var quick = MeasureQuick(words, repetitions);
        var radix = MeasureRadix(words, repetitions);

        var quickCheck = (string[])words.Clone();
        QuickSorter.Sort(quickCheck);
        var radixCheck = (string[])words.Clone();
        RadixSorter.Sort(radixCheck);
        var counts = WordAnalyzer.CountSorted(quickCheck);

        return new BenchmarkResult(
            wordCount,
            quick.Milliseconds,
            radix.Milliseconds,
            quick.Metrics.Comparisons,
            radix.Operations,
            counts.Count,
            quickCheck.AsSpan().SequenceEqual(radixCheck));
    }

    private static (double Milliseconds, SortMetrics Metrics) MeasureQuick(string[] source, int repetitions)
    {
        var best = double.MaxValue;
        var bestMetrics = new SortMetrics(0, 0);

        for (var repetition = 0; repetition < repetitions; repetition++)
        {
            var copy = (string[])source.Clone();
            var stopwatch = Stopwatch.StartNew();
            var metrics = QuickSorter.Sort(copy);
            stopwatch.Stop();

            if (stopwatch.Elapsed.TotalMilliseconds < best)
            {
                best = stopwatch.Elapsed.TotalMilliseconds;
                bestMetrics = metrics;
            }
        }

        return (best, bestMetrics);
    }

    private static (double Milliseconds, long Operations) MeasureRadix(string[] source, int repetitions)
    {
        var best = double.MaxValue;
        long bestOperations = 0;

        for (var repetition = 0; repetition < repetitions; repetition++)
        {
            var copy = (string[])source.Clone();
            var stopwatch = Stopwatch.StartNew();
            var operations = RadixSorter.Sort(copy);
            stopwatch.Stop();

            if (stopwatch.Elapsed.TotalMilliseconds < best)
            {
                best = stopwatch.Elapsed.TotalMilliseconds;
                bestOperations = operations;
            }
        }

        return (best, bestOperations);
    }
}
