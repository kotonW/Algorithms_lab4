using AlgorithmsLab4.Core.Sorting;

namespace AlgorithmsLab4.Tests;

public sealed class SortRunnerTests
{
    public static TheoryData<SortAlgorithm> Algorithms =>
    [
        SortAlgorithm.SelectSort,
        SortAlgorithm.BubbleSort,
        SortAlgorithm.HeapSort,
        SortAlgorithm.QuickSort,
    ];

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Sort_ProducesAscendingArray_ForRandomData(SortAlgorithm algorithm)
    {
        var random = new Random(42);

        for (var iteration = 0; iteration < 80; iteration++)
        {
            var size = random.Next(0, 101);
            var values = new int[size];
            for (var index = 0; index < size; index++)
            {
                values[index] = random.Next(0, 30);
            }

            var expected = (int[])values.Clone();
            Array.Sort(expected);

            var events = SortRunner.Run(algorithm, values).ToList();

            Assert.Equal(expected, values);
            Assert.All(events, sortEvent =>
            {
                Assert.Equal(values.Length, sortEvent.Values.Length);
                Assert.False(string.IsNullOrWhiteSpace(sortEvent.Explanation));
            });
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Sort_LogsEveryComparisonAndSwap(SortAlgorithm algorithm)
    {
        var values = new[] { 5, 1, 4, 2, 8 };
        var events = SortRunner.Run(algorithm, values).ToList();

        foreach (var sortEvent in events.Where(sortEvent =>
                     sortEvent.Kind is SortEventKind.Compare or SortEventKind.Swap))
        {
            Assert.False(string.IsNullOrWhiteSpace(sortEvent.LogText));
        }

        Assert.Equal(new[] { 1, 2, 4, 5, 8 }, values);
    }
}
