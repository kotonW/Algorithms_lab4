using AlgorithmsLab4.Core.Words;

namespace AlgorithmsLab4.Tests;

public sealed class WordSorterTests
{
    [Fact]
    public void QuickAndRadixSort_ProduceSameLexicographicOrder()
    {
        var words = TextCorpus.CreateWords(5000, seed: 17);

        var quick = (string[])words.Clone();
        QuickSorter.Sort(quick);

        var radix = (string[])words.Clone();
        RadixSorter.Sort(radix);

        var expected = (string[])words.Clone();
        Array.Sort(expected, StringComparer.Ordinal);

        Assert.Equal(expected, quick);
        Assert.Equal(expected, radix);

        for (var index = 1; index < quick.Length; index++)
        {
            Assert.True(string.CompareOrdinal(quick[index - 1], quick[index]) <= 0);
        }
    }

    [Fact]
    public void Tokenizer_IgnoresPunctuationAndNormalizesCase()
    {
        var words = WordTokenizer.Tokenize("Don't stop! The FOX, the dog... and the fox.");

        Assert.Equal(["don't", "stop", "the", "fox", "the", "dog", "and", "the", "fox"], words);
    }

    [Fact]
    public void CountSorted_CountsAdjacentDuplicates()
    {
        string[] words = ["apple", "apple", "banana", "cherry", "cherry", "cherry"];

        var counts = WordAnalyzer.CountSorted(words);

        Assert.Equal(
            [new WordCount("apple", 2), new WordCount("banana", 1), new WordCount("cherry", 3)],
            counts);
    }

    [Fact]
    public void Analyze_ReturnsMatchingResultsAndCountsEveryWord()
    {
        var analysis = WordAnalyzer.Analyze(TextCorpus.SampleText);

        Assert.True(analysis.ResultsMatch);
        Assert.Equal(analysis.Words.Length, analysis.QuickSorted.Length);
        Assert.Equal(analysis.Words.Length, analysis.QuickSorted.Length);
        Assert.Equal(analysis.QuickSorted.Length, analysis.Counts.Sum(count => count.Count));
        Assert.Equal(analysis.Words.Length, analysis.Counts.Sum(count => count.Count));
        Assert.True(analysis.QuickMilliseconds >= 0);
        Assert.True(analysis.RadixMilliseconds >= 0);
    }

    [Fact]
    public void Benchmark_ProducesMatchingResults()
    {
        var result = BenchmarkRunner.RunOne(1000, repetitions: 1);

        Assert.Equal(1000, result.WordCount);
        Assert.True(result.ResultsMatch);
        Assert.True(result.QuickMilliseconds >= 0);
        Assert.True(result.RadixMilliseconds >= 0);
        Assert.True(result.UniqueWords > 0);
    }
}
