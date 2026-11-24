namespace AlgorithmsLab4.Core.Words;

/// <summary>Сколько раз слово встретилось в тексте.</summary>
public sealed record WordCount(string Word, int Count);

/// <summary>Статистика одного запуска сортировки.</summary>
public sealed record SortMetrics(long Comparisons, long Swaps);
