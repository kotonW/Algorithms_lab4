namespace AlgorithmsLab4.Core.ExternalSort;

/// <summary>
/// Тип шага внешней сортировки — используется для журнала и визуального пояснения.
/// </summary>
public enum ExternalSortStepKind
{
    Info,
    Comparison,
    Move,
    RunCreated,
    MergePass,
    Completed,
}

/// <summary>
/// Один шаг внешней сортировки с накопленной статистикой.
/// </summary>
public sealed record ExternalSortStep
{
    public required ExternalSortStepKind Kind { get; init; }

    /// <summary>Короткое пояснение текущего этапа для панели «Что происходит».</summary>
    public required string Title { get; init; }

    /// <summary>Подробная строка журнала. Для информационных шагов может быть пустой.</summary>
    public string LogText { get; init; } = string.Empty;

    public long Comparisons { get; init; }

    public long Moves { get; init; }

    public int RunsCreated { get; init; }

    public int MergePasses { get; init; }

    public ExternalSortResult? Result { get; init; }
}

/// <summary>
/// Итог внешней сортировки.
/// </summary>
public sealed record ExternalSortResult(
    string OutputPath,
    int RecordCount,
    int FilteredOut,
    long Comparisons,
    long Moves,
    int RunsCreated,
    int MergePasses,
    ExternalSortMethod Method,
    string KeyColumn,
    bool Descending);
