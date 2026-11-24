using System.Text.RegularExpressions;

namespace AlgorithmsLab4.Core.Words;

/// <summary>
/// Разбивает текст на слова и приводит их к нижнему регистру. Дефисы и
/// апострофы внутри слова сохраняются, знаки препинания отбрасываются.
/// </summary>
public static partial class WordTokenizer
{
    [GeneratedRegex(@"[\p{L}\p{M}]+(?:['’\-][\p{L}\p{M}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();

    public static string[] Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var words = new List<string>();
        foreach (Match match in WordPattern().Matches(text))
        {
            var word = match.Value
                .Replace('’', '\'')
                .ToLowerInvariant();
            words.Add(word);
        }

        return [.. words];
    }
}
