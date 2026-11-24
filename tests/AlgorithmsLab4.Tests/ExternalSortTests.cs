using AlgorithmsLab4.Core;
using AlgorithmsLab4.Core.ExternalSort;
using System.Text;

namespace AlgorithmsLab4.Tests;

public sealed class ExternalSortTests
{
    public static TheoryData<ExternalSortMethod> Methods =>
    [
        ExternalSortMethod.DirectMerge,
        ExternalSortMethod.NaturalMerge,
        ExternalSortMethod.MultiwayMerge,
    ];

    [Theory]
    [MemberData(nameof(Methods))]
    public void Sort_CountriesByPopulation_FiltersEurope(ExternalSortMethod method)
    {
        var directory = CreateTempDirectory();
        try
        {
            var input = Path.Combine(directory, "countries.csv");
            var output = Path.Combine(directory, "countries_sorted.csv");
            SampleData.SaveCountriesSample(input);

            var options = new ExternalSortOptions
            {
                KeyColumnIndex = 4,
                KeyColumnName = "Население",
                Method = method,
                RunSize = 5,
                FanIn = 3,
                FilterColumnIndex = 1,
                FilterValue = "Европа",
                TempDirectoryRoot = Path.Combine(directory, "temp"),
            };

            var steps = ExternalSorter.Sort(input, output, options).ToList();
            var result = Assert.IsType<ExternalSortResult>(steps[^1].Result);

            Assert.Equal(14, result.RecordCount);
            Assert.True(result.Comparisons > 0);
            Assert.True(result.Moves > 0);
            Assert.True(result.RunsCreated > 1);
            Assert.True(result.MergePasses > 0);
            Assert.Contains(steps, step => step.Kind == ExternalSortStepKind.Comparison);
            Assert.Contains(steps, step => step.Kind == ExternalSortStepKind.Move);

            var table = CsvTable.Load(output);
            Assert.Equal(result.RecordCount, table.Rows.Count);
            Assert.All(table.Rows, row => Assert.Equal("Европа", row[1]));

            var populations = table.Rows.Select(row => decimal.Parse(row[4], System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            Assert.Equal(populations.OrderBy(value => value), populations);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Sort_ByNameDescending_WorksForAllMethods()
    {
        var directory = CreateTempDirectory();
        try
        {
            var input = Path.Combine(directory, "countries.csv");
            SampleData.SaveCountriesSample(input);

            foreach (var method in new[]
                     {
                         ExternalSortMethod.DirectMerge,
                         ExternalSortMethod.NaturalMerge,
                         ExternalSortMethod.MultiwayMerge,
                     })
            {
                var output = Path.Combine(directory, $"{method}.csv");
                var options = new ExternalSortOptions
                {
                    KeyColumnIndex = 0,
                    KeyColumnName = "Страна",
                    Descending = true,
                    Method = method,
                    RunSize = 4,
                    FanIn = 2,
                    TempDirectoryRoot = Path.Combine(directory, "temp"),
                };

                var steps = ExternalSorter.Sort(input, output, options).ToList();
                var result = Assert.IsType<ExternalSortResult>(steps[^1].Result);
                Assert.Equal(38, result.RecordCount);

                var table = CsvTable.Load(output);
                var names = table.Rows.Select(row => row[0]).ToArray();
                Assert.Equal(names.OrderByDescending(name => name, StringComparer.CurrentCultureIgnoreCase), names);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Csv_RoundTripsQuotedFieldsAndNewLines()
    {
        var directory = CreateTempDirectory();
        try
        {
            var input = Path.Combine(directory, "quoted.csv");
            File.WriteAllText(
                input,
                "Страна;Описание;Число\r\n" +
                "\"Страна; с разделителем\";\"Многострочное\nописание \"\"в кавычках\"\"\";42\r\n",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            var table = CsvTable.Load(input);
            Assert.Equal(3, table.ColumnCount);
            Assert.Single(table.Rows);
            Assert.Equal("Страна; с разделителем", table.Rows[0][0]);
            Assert.Equal("Многострочное\nописание \"в кавычках\"", table.Rows[0][1]);
            Assert.Equal("42", table.Rows[0][2]);

            var output = Path.Combine(directory, "copy.csv");
            table.Save(output);
            var copy = CsvTable.Load(output);

            Assert.Equal(table.Headers, copy.Headers);
            Assert.Equal(table.Rows[0], copy.Rows[0]);
            Assert.False(string.IsNullOrWhiteSpace(table.ToPreviewText()));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Sort_EmptyFilterResult_CreatesHeaderOnlyFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            var input = Path.Combine(directory, "countries.csv");
            var output = Path.Combine(directory, "empty.csv");
            SampleData.SaveCountriesSample(input);

            var options = new ExternalSortOptions
            {
                KeyColumnIndex = 4,
                KeyColumnName = "Население",
                Method = ExternalSortMethod.DirectMerge,
                RunSize = 8,
                FilterColumnIndex = 1,
                FilterValue = "Антарктида",
                TempDirectoryRoot = Path.Combine(directory, "temp"),
            };

            var steps = ExternalSorter.Sort(input, output, options).ToList();
            var result = Assert.IsType<ExternalSortResult>(steps[^1].Result);

            Assert.Equal(0, result.RecordCount);
            Assert.True(File.Exists(output));
            var table = CsvTable.Load(output);
            Assert.Empty(table.Rows);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "test-temp",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
