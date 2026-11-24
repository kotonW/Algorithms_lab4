namespace AlgorithmsLab4.Core.ExternalSort;

/// <summary>
/// Параметры внешней сортировки CSV-таблицы.
/// </summary>
public sealed record ExternalSortOptions
{
    /// <summary>Индекс колонки, по которой сравниваются записи.</summary>
    public required int KeyColumnIndex { get; init; }

    /// <summary>Имя ключевой колонки (для журнала и итогового отчёта).</summary>
    public string KeyColumnName { get; init; } = string.Empty;

    /// <summary>Сортировать по убыванию?</summary>
    public bool Descending { get; init; }

    public ExternalSortMethod Method { get; init; } = ExternalSortMethod.DirectMerge;

    /// <summary>Размер блока, который сортируется в оперативной памяти за один раз.</summary>
    public int RunSize { get; init; } = 8;

    /// <summary>Количество серий, сливаемых за один проход (для многопутевого слияния).</summary>
    public int FanIn { get; init; } = 4;

    /// <summary>Индекс колонки фильтра или -1, если фильтр не нужен.</summary>
    public int FilterColumnIndex { get; init; } = -1;

    /// <summary>Значение фильтра (сравнивается без учёта регистра).</summary>
    public string? FilterValue { get; init; }

    /// <summary>
    /// Каталог для временных серий. Если не задан, используется
    /// %LOCALAPPDATA%\AlgorithmsLab4\temp.
    /// </summary>
    public string? TempDirectoryRoot { get; init; }
}
