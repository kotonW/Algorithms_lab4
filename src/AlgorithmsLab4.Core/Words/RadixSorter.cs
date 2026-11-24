namespace AlgorithmsLab4.Core.Words;

/// <summary>
/// Поразрядная сортировка строк (LSD radix sort). Слова выравниваются по
/// длине нулевым символом, после чего стабильно сортируются по каждому
/// разряду справа налево. Нулевой символ меньше любой буквы, поэтому более
/// короткое слово с тем же началом оказывается раньше — как при обычном
/// лексикографическом сравнении.
/// </summary>
public static class RadixSorter
{
    private const int AlphabetSize = char.MaxValue + 1;

    /// <summary>
    /// Сортирует массив на месте. Возвращает количество операций с символами.
    /// </summary>
    public static long Sort(string[] words)
    {
        ArgumentNullException.ThrowIfNull(words);
        if (words.Length < 2)
        {
            return 0;
        }

        var maxLength = 0;
        foreach (var word in words)
        {
            maxLength = Math.Max(maxLength, word.Length);
        }

        if (maxLength == 0)
        {
            return 0;
        }

        var buffer = new string[words.Length];
        var counts = new int[AlphabetSize + 1];
        var source = words;
        var target = buffer;
        long operations = 0;

        for (var position = maxLength - 1; position >= 0; position--)
        {
            Array.Clear(counts);

            foreach (var word in source)
            {
                var key = position < word.Length ? word[position] : '\0';
                counts[key + 1]++;
            }

            for (var key = 1; key < counts.Length; key++)
            {
                counts[key] += counts[key - 1];
            }

            foreach (var word in source)
            {
                var key = position < word.Length ? word[position] : '\0';
                target[counts[key]++] = word;
            }

            operations += source.Length;
            (source, target) = (target, source);
        }

        if (!ReferenceEquals(source, words))
        {
            Array.Copy(source, words, words.Length);
        }

        return operations;
    }
}
