namespace AlgorithmsLab4.Core.Sorting;

/// <summary>
/// Алгоритмы внутренней сортировки, которые демонстрируются в задании 1.
/// </summary>
public enum SortAlgorithm
{
    /// <summary>Сортировка выбором.</summary>
    SelectSort,

    /// <summary>Сортировка пузырьком с ранним выходом и запоминанием последнего обмена.</summary>
    BubbleSort,

    /// <summary>Пирамидальная сортировка (бинарная пирамида).</summary>
    HeapSort,

    /// <summary>Быстрая сортировка Хоара (схема Ломуто).</summary>
    QuickSort,
}
