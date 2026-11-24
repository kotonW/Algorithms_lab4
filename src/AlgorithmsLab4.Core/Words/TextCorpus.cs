namespace AlgorithmsLab4.Core.Words;

/// <summary>
/// Встроенный английский текст и генератор текстов разной длины для
/// экспериментов из задания 3.
/// </summary>
public static class TextCorpus
{
    /// <summary>Небольшой текст для быстрого знакомства с заданием.</summary>
    public const string SampleText =
        "Sorting words is a classic task in computer science. " +
        "We take a text, split it into words, and put the words in alphabetical order. " +
        "When equal words stand next to each other, counting how often each word appears becomes easy. " +
        "Quick sort chooses a pivot and divides the list into smaller parts. " +
        "Radix sort looks at one character at a time, from the last character to the first. " +
        "The two methods often produce the same sorted list, but they take different paths to get there. " +
        "Sorting is fun when you can see every step, and it is even more fun when you can compare " +
        "two algorithms on texts of different sizes. A good experiment uses the same words for both " +
        "algorithms, measures the time, and writes the result in a table. The table then shows which " +
        "method is faster for small texts and which method wins for large texts.";

    private static readonly string[] Vocabulary =
    [
        "the", "of", "and", "to", "in", "a", "is", "that", "for", "it",
        "as", "was", "with", "be", "by", "on", "not", "this", "are", "or",
        "from", "at", "which", "but", "have", "an", "had", "they", "you", "were",
        "their", "one", "all", "we", "can", "has", "there", "if", "more", "when",
        "will", "would", "who", "so", "no", "she", "other", "its", "may", "what",
        "up", "out", "about", "into", "than", "them", "these", "some", "could", "time",
        "words", "sort", "text", "list", "quick", "radix", "algorithm", "data", "step", "table",
        "method", "small", "large", "compare", "result", "computer", "science", "word", "letter", "character",
        "order", "same", "different", "fast", "slow", "first", "last", "good", "new", "see",
        "use", "make", "take", "find", "work", "part", "place", "number", "group", "example",
        "school", "student", "teacher", "book", "read", "write", "learn", "lesson", "problem", "solution",
        "simple", "easy", "hard", "begin", "end", "show", "count", "equal", "next", "each",
        "every", "many", "much", "few", "most", "less", "least", "big", "little", "long",
        "short", "high", "low", "old", "young", "day", "night", "year", "week", "world",
        "country", "city", "house", "family", "friend", "people", "man", "woman", "child", "life",
        "water", "light", "dark", "red", "blue", "green", "sun", "moon", "star", "tree",
        "river", "sea", "road", "music", "game", "story", "idea", "question", "answer", "help",
        "need", "want", "like", "look", "come", "go", "give", "know", "think", "say",
    ];

    /// <summary>
    /// Генерирует массив слов. Распределение слегка неравномерное, как в
    /// настоящем тексте: частые слова встречаются гораздо чаще редких.
    /// </summary>
    public static string[] CreateWords(int count, int seed = 20240501)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var random = new Random(seed);
        var words = new string[count];

        for (var index = 0; index < count; index++)
        {
            var value = Math.Pow(random.NextDouble(), 2.15);
            var vocabularyIndex = Math.Min(Vocabulary.Length - 1, (int)(value * Vocabulary.Length));
            words[index] = Vocabulary[vocabularyIndex];
        }

        return words;
    }

    /// <summary>
    /// Строит массив нужной длины из загруженного текста (слова повторяются),
    /// либо из встроенного генератора. Порядок перемешивается, чтобы оба
    /// алгоритма сортировали одинаковый неупорядоченный набор.
    /// </summary>
    public static string[] BuildWordList(int count, IReadOnlyList<string>? sourceWords, int seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var words = sourceWords is { Count: > 0 }
            ? BuildFromSource(count, sourceWords)
            : CreateWords(count, seed);

        Shuffle(words, seed + count);
        return words;
    }

    private static string[] BuildFromSource(int count, IReadOnlyList<string> sourceWords)
    {
        var words = new string[count];
        for (var index = 0; index < count; index++)
        {
            words[index] = sourceWords[index % sourceWords.Count];
        }

        return words;
    }

    private static void Shuffle(string[] words, int seed)
    {
        var random = new Random(seed);
        for (var index = words.Length - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (words[index], words[swapIndex]) = (words[swapIndex], words[index]);
        }
    }
}
