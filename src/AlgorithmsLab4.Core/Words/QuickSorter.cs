namespace AlgorithmsLab4.Core.Words;

/// <summary>
/// Быстрая сортировка строк (QuickSort) с трёхпутевым разделением.
/// Трёхпутевая схема особенно полезна для текстов: одинаковые слова
/// не попадают в рекурсию и обрабатываются за один проход.
/// </summary>
public static class QuickSorter
{
    private const int SmallRangeThreshold = 16;

    public static SortMetrics Sort(string[] words)
    {
        ArgumentNullException.ThrowIfNull(words);
        long comparisons = 0;
        long swaps = 0;

        if (words.Length > 1)
        {
            SortRange(words, 0, words.Length - 1, ref comparisons, ref swaps);
        }

        return new SortMetrics(comparisons, swaps);
    }

    private static void SortRange(
        string[] words,
        int low,
        int high,
        ref long comparisons,
        ref long swaps)
    {
        while (low < high)
        {
            if (high - low < SmallRangeThreshold)
            {
                InsertionSort(words, low, high, ref comparisons, ref swaps);
                return;
            }

            // Опорный элемент — середина диапазона: для уже упорядоченных
            // массивов это гораздо лучше, чем первый или последний элемент.
            var pivot = words[low + ((high - low) / 2)];
            var less = low;
            var index = low;
            var greater = high;

            while (index <= greater)
            {
                var comparison = string.CompareOrdinal(words[index], pivot);
                comparisons++;

                if (comparison < 0)
                {
                    if (less != index)
                    {
                        Swap(words, less, index);
                        swaps++;
                    }

                    less++;
                    index++;
                }
                else if (comparison > 0)
                {
                    if (index != greater)
                    {
                        Swap(words, index, greater);
                        swaps++;
                    }

                    greater--;
                }
                else
                {
                    index++;
                }
            }

            // Рекурсивно обрабатываем меньшую часть, а большую — циклом.
            // Так глубина стека остаётся небольшой.
            if (less - low < high - greater)
            {
                SortRange(words, low, less - 1, ref comparisons, ref swaps);
                low = greater + 1;
            }
            else
            {
                SortRange(words, greater + 1, high, ref comparisons, ref swaps);
                high = less - 1;
            }
        }
    }

    private static void InsertionSort(
        string[] words,
        int low,
        int high,
        ref long comparisons,
        ref long swaps)
    {
        for (var index = low + 1; index <= high; index++)
        {
            var current = words[index];
            var position = index - 1;

            while (position >= low)
            {
                var comparison = string.CompareOrdinal(words[position], current);
                comparisons++;
                if (comparison <= 0)
                {
                    break;
                }

                words[position + 1] = words[position];
                swaps++;
                position--;
            }

            words[position + 1] = current;
        }
    }

    private static void Swap(string[] words, int left, int right)
        => (words[left], words[right]) = (words[right], words[left]);
}
