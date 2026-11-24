namespace AlgorithmsLab4.Core.Sorting;

/// <summary>
/// Реализации четырёх алгоритмов внутренней сортировки. Каждый алгоритм —
/// итератор: он не только сортирует массив, но и «отдаёт» по одному событию,
/// чтобы графический интерфейс мог показать процесс по шагам.
/// </summary>
public static class SortRunner
{
    /// <summary>
    /// Запускает выбранный алгоритм. Переданный массив сортируется на месте.
    /// </summary>
    public static IEnumerable<SortEvent> Run(SortAlgorithm algorithm, int[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return algorithm switch
        {
            SortAlgorithm.SelectSort => SelectSort(values),
            SortAlgorithm.BubbleSort => BubbleSort(values),
            SortAlgorithm.HeapSort => HeapSort(values),
            SortAlgorithm.QuickSort => QuickSort(values),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Неизвестный алгоритм сортировки."),
        };
    }

    /// <summary>
    /// Сортировка выбором. На каждом проходе находим минимум в неотсортированной
    /// части и ставим его в начало этой части.
    /// </summary>
    private static IEnumerable<SortEvent> SelectSort(int[] values)
    {
        var n = values.Length;
        yield return SortEvent.Message(
            values,
            "Сортировка выбором: на каждом проходе ищем самый маленький элемент и ставим его в начало.");

        for (var start = 0; start < n - 1; start++)
        {
            var minIndex = start;
            yield return SortEvent.Message(
                values,
                $"Проход {start + 1}: ищем минимум среди элементов с {start}-го по {n - 1}-й.");

            for (var index = start + 1; index < n; index++)
            {
                yield return SortEvent.Compare(values, minIndex, index);

                if (values[index] < values[minIndex])
                {
                    minIndex = index;
                }
            }

            if (minIndex != start)
            {
                yield return SortEvent.Swap(
                    values,
                    start,
                    minIndex,
                    $"Минимум найден: {values[minIndex]}. Ставим его на позицию {start}.");
            }
            else
            {
                yield return SortEvent.Message(
                    values,
                    $"Минимум уже стоит на позиции {start} — переставлять не нужно.");
            }

            yield return SortEvent.MarkSorted(values, start);
        }

        if (n > 0)
        {
            yield return SortEvent.MarkSorted(values, n - 1);
        }
    }

    /// <summary>
    /// Сортировка пузырьком с двумя улучшениями: ранний выход, если обменов
    /// не было, и запоминание последнего обмена (хвост сразу считается готовым).
    /// </summary>
    private static IEnumerable<SortEvent> BubbleSort(int[] values)
    {
        var n = values.Length;
        yield return SortEvent.Message(
            values,
            "Сортировка пузырьком: большие числа «всплывают» вправо, как пузырьки в воде.");

        var end = n - 1;
        while (end > 0)
        {
            var swapped = false;
            var lastSwap = 0;

            yield return SortEvent.Message(
                values,
                $"Проход по элементам с 0-го по {end}-й. Если сосед слева больше соседа справа — меняем их.");

            for (var index = 0; index < end; index++)
            {
                yield return SortEvent.Compare(values, index, index + 1);

                if (values[index] > values[index + 1])
                {
                    yield return SortEvent.Swap(values, index, index + 1);
                    swapped = true;
                    lastSwap = index;
                }
            }

            var oldEnd = end;
            if (!swapped)
            {
                for (var index = oldEnd; index >= 0; index--)
                {
                    yield return SortEvent.MarkSorted(values, index);
                }

                yield break;
            }

            // Всё, что правее последнего обмена, уже стоит правильно.
            for (var index = oldEnd; index > lastSwap; index--)
            {
                yield return SortEvent.MarkSorted(values, index);
            }

            end = lastSwap;
        }

        if (n > 0)
        {
            yield return SortEvent.MarkSorted(values, 0);
        }
    }

    /// <summary>
    /// Пирамидальная сортировка: сначала строим max-пирамиду, затем по одному
    /// вынимаем максимум из корня и ставим его в конец массива.
    /// </summary>
    private static IEnumerable<SortEvent> HeapSort(int[] values)
    {
        var n = values.Length;
        yield return SortEvent.Message(
            values,
            "Пирамидальная сортировка: представляем массив как пирамиду, где родитель не меньше детей.");

        yield return SortEvent.Message(
            values,
            "Шаг 1. Строим пирамиду снизу вверх, начиная с последнего родителя.");

        for (var root = n / 2 - 1; root >= 0; root--)
        {
            foreach (var sortEvent in SiftDown(values, root, n))
            {
                yield return sortEvent;
            }
        }

        yield return SortEvent.Message(
            values,
            "Шаг 2. В корне пирамиды теперь максимум. Меняем его с последним элементом и восстанавливаем пирамиду.");

        for (var end = n - 1; end > 0; end--)
        {
            yield return SortEvent.Swap(
                values,
                0,
                end,
                $"Максимум {values[0]} переносим в конец неотсортированной части (позиция {end}).");
            yield return SortEvent.MarkSorted(values, end);

            foreach (var sortEvent in SiftDown(values, 0, end))
            {
                yield return sortEvent;
            }
        }

        if (n > 0)
        {
            yield return SortEvent.MarkSorted(values, 0);
        }
    }

    /// <summary>
    /// «Просеивает» элемент вниз по пирамиде, восстанавливая свойство max-пирамиды.
    /// </summary>
    private static IEnumerable<SortEvent> SiftDown(int[] values, int root, int count)
    {
        while (true)
        {
            var child = root * 2 + 1;
            if (child >= count)
            {
                yield break;
            }

            if (child + 1 < count)
            {
                yield return SortEvent.Compare(values, child, child + 1);

                if (values[child + 1] > values[child])
                {
                    child++;
                }
            }

            yield return SortEvent.Compare(values, root, child);

            if (values[root] >= values[child])
            {
                yield break;
            }

            yield return SortEvent.Swap(
                values,
                root,
                child,
                $"Родитель {values[root]} меньше ребёнка {values[child]} — поднимаем ребёнка выше.");
            root = child;
        }
    }

    /// <summary>
    /// Быстрая сортировка Хоара со схемой разделения Ломуто. В качестве опорного
    /// выбран последний элемент диапазона; рекурсия заменена стеком.
    /// </summary>
    private static IEnumerable<SortEvent> QuickSort(int[] values)
    {
        if (values.Length == 0)
        {
            yield break;
        }

        yield return SortEvent.Message(
            values,
            "Быстрая сортировка: выбираем опорный элемент и делим массив на «меньшие» и «большие».");

        var stack = new Stack<(int Low, int High)>();
        stack.Push((0, values.Length - 1));

        while (stack.Count > 0)
        {
            var (low, high) = stack.Pop();
            if (low > high)
            {
                continue;
            }

            if (low == high)
            {
                yield return SortEvent.MarkSorted(values, low);
                continue;
            }

            var pivot = values[high];
            yield return SortEvent.Pivot(
                values,
                high,
                $"Опорный элемент a[{high}] = {pivot}. Всё, что меньше, отправим влево, а больше — вправо.");

            var smaller = low - 1;
            for (var index = low; index < high; index++)
            {
                yield return SortEvent.Compare(values, index, high);

                if (values[index] <= pivot)
                {
                    smaller++;
                    if (smaller != index)
                    {
                        yield return SortEvent.Swap(values, smaller, index);
                    }
                }
            }

            var pivotPosition = smaller + 1;
            if (pivotPosition != high)
            {
                yield return SortEvent.Swap(
                    values,
                    pivotPosition,
                    high,
                    $"Ставим опорный элемент {pivot} на его окончательное место — позицию {pivotPosition}.");
            }

            yield return SortEvent.MarkSorted(
                values,
                pivotPosition,
                $"Опорный элемент {pivot} теперь на своём месте: слева — меньшие, справа — большие.");

            // Сначала складываем правую часть, потом левую: так меньшая часть
            // обрабатывается первой, но на результат это не влияет.
            stack.Push((pivotPosition + 1, high));
            stack.Push((low, pivotPosition - 1));
        }
    }
}
