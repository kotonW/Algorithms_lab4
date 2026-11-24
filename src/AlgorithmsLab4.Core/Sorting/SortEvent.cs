namespace AlgorithmsLab4.Core.Sorting;

/// <summary>
/// Тип события, которое алгоритм сообщает визуализатору.
/// </summary>
public enum SortEventKind
{
    /// <summary>Сравнение двух элементов.</summary>
    Compare,

    /// <summary>Перестановка двух элементов.</summary>
    Swap,

    /// <summary>Элемент окончательно встал на своё место.</summary>
    MarkSorted,

    /// <summary>Выбран опорный элемент быстрой сортировки.</summary>
    Pivot,

    /// <summary>Пояснение текущего этапа алгоритма.</summary>
    Message,
}

/// <summary>
/// Один шаг сортировки. Событие содержит снимок массива после выполнения шага,
/// поэтому интерфейс может отрисовать состояние, не заглядывая внутрь алгоритма.
/// </summary>
public sealed class SortEvent
{
    private SortEvent()
    {
    }

    public SortEventKind Kind { get; private init; }

    /// <summary>Снимок массива после шага.</summary>
    public int[] Values { get; private init; } = [];

    public int IndexA { get; private init; } = -1;

    public int IndexB { get; private init; } = -1;

    public int ValueA { get; private init; }

    public int ValueB { get; private init; }

    /// <summary>Индекс элемента, который считается отсортированным.</summary>
    public int SortedIndex { get; private init; } = -1;

    /// <summary>Понятное объяснение для ученика 6 класса.</summary>
    public string Explanation { get; private init; } = string.Empty;

    /// <summary>Строка для журнала сравнений и перестановок. Может быть пустой.</summary>
    public string LogText { get; private init; } = string.Empty;

    /// <summary>
    /// Создаёт событие сравнения. Отношение элементов вычисляется автоматически,
    /// чтобы журнал всегда соответствовал реальным данным.
    /// </summary>
    public static SortEvent Compare(int[] values, int indexA, int indexB, string? explanation = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var valueA = values[indexA];
        var valueB = values[indexB];
        var sign = valueA == valueB ? "=" : valueA > valueB ? ">" : "<";
        var relation = valueA == valueB
            ? "они равны"
            : valueA > valueB
                ? $"{valueA} больше {valueB}"
                : $"{valueA} меньше {valueB}";

        return new SortEvent
        {
            Kind = SortEventKind.Compare,
            Values = (int[])values.Clone(),
            IndexA = indexA,
            IndexB = indexB,
            ValueA = valueA,
            ValueB = valueB,
            Explanation = explanation ?? $"Сравниваем a[{indexA}] = {valueA} и a[{indexB}] = {valueB}: {relation}.",
            LogText = $"Сравнение: a[{indexA}] = {valueA} {sign} a[{indexB}] = {valueB}",
        };
    }

    /// <summary>
    /// Переставляет два элемента и создаёт событие перестановки.
    /// </summary>
    public static SortEvent Swap(int[] values, int indexA, int indexB, string? explanation = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var valueA = values[indexA];
        var valueB = values[indexB];
        (values[indexA], values[indexB]) = (values[indexB], values[indexA]);

        return new SortEvent
        {
            Kind = SortEventKind.Swap,
            Values = (int[])values.Clone(),
            IndexA = indexA,
            IndexB = indexB,
            ValueA = valueA,
            ValueB = valueB,
            Explanation = explanation ?? $"Меняем местами a[{indexA}] = {valueA} и a[{indexB}] = {valueB}.",
            LogText = $"Перестановка: a[{indexA}] = {valueA} ↔ a[{indexB}] = {valueB}",
        };
    }

    /// <summary>
    /// Помечает элемент как окончательно отсортированный.
    /// </summary>
    public static SortEvent MarkSorted(int[] values, int index, string? explanation = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new SortEvent
        {
            Kind = SortEventKind.MarkSorted,
            Values = (int[])values.Clone(),
            SortedIndex = index,
            ValueA = values[index],
            Explanation = explanation ?? $"Элемент a[{index}] = {values[index]} уже стоит на своём месте.",
        };
    }

    /// <summary>
    /// Помечает выбранный опорный элемент.
    /// </summary>
    public static SortEvent Pivot(int[] values, int index, string? explanation = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new SortEvent
        {
            Kind = SortEventKind.Pivot,
            Values = (int[])values.Clone(),
            IndexA = index,
            ValueA = values[index],
            Explanation = explanation ?? $"Опорный элемент — a[{index}] = {values[index]}.",
        };
    }

    /// <summary>
    /// Создаёт пояснение к этапу алгоритма. В журнал такие события не попадают.
    /// </summary>
    public static SortEvent Message(int[] values, string explanation)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new SortEvent
        {
            Kind = SortEventKind.Message,
            Values = (int[])values.Clone(),
            Explanation = explanation,
        };
    }
}
