namespace AlgorithmsLab4.Core.ExternalSort;

/// <summary>
/// Метод внешней сортировки, выбранный пользователем.
/// </summary>
public enum ExternalSortMethod
{
    /// <summary>Прямое (двухпутевое) слияние с блоками фиксированного размера.</summary>
    DirectMerge,

    /// <summary>Естественное слияние: сначала выделяются уже упорядоченные серии.</summary>
    NaturalMerge,

    /// <summary>Многопутевое слияние: за один проход сливаются сразу несколько серий.</summary>
    MultiwayMerge,
}
