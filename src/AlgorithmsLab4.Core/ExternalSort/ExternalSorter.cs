namespace AlgorithmsLab4.Core.ExternalSort;

/// <summary>
/// Внешняя сортировка CSV-таблицы методом слияния. Входной файл читается
/// потоково, промежуточные серии хранятся во временных файлах, а результат
/// записывается в отдельный CSV-файл с тем же набором колонок.
/// </summary>
public static class ExternalSorter
{
    /// <summary>
    /// Выполняет сортировку и возвращает шаги для пошаговой визуализации.
    /// Итоговая информация находится в последнем шаге (<see cref="ExternalSortStep.Result"/>).
    /// </summary>
    public static IEnumerable<ExternalSortStep> Sort(
        string inputPath,
        string outputPath,
        ExternalSortOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(options);

        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Входной CSV-файл не найден.", inputPath);
        }

        if (string.Equals(Path.GetFullPath(inputPath), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Итоговый файл должен отличаться от входного.", nameof(outputPath));
        }

        if (options.RunSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Размер блока должен быть положительным.");
        }

        var delimiter = CsvTable.DetectDelimiter(inputPath);
        string[] headers;
        using (var headerReader = new CsvRecordReader(inputPath, delimiter))
        {
            headers = headerReader.Headers;
        }

        if (headers.Length == 0)
        {
            throw new InvalidDataException("В файле нет заголовка таблицы.");
        }

        if (options.KeyColumnIndex < 0 || options.KeyColumnIndex >= headers.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Ключевая колонка отсутствует в таблице.");
        }

        if (options.FilterColumnIndex >= headers.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Колонка фильтра отсутствует в таблице.");
        }

        var numeric = IsNumericKey(inputPath, delimiter, options.KeyColumnIndex);
        var comparer = new ExternalRecordComparer(
            options.KeyColumnIndex,
            options.KeyColumnName,
            numeric,
            options.Descending);
        var tempRoot = string.IsNullOrWhiteSpace(options.TempDirectoryRoot)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AlgorithmsLab4",
                "temp")
            : options.TempDirectoryRoot;
        var tempDirectory = Path.Combine(tempRoot, $"external_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        var context = new SortContext(headers, options, delimiter, comparer, tempDirectory);

        try
        {
            yield return Info(
                context,
                $"Читаем файл «{Path.GetFileName(inputPath)}»: {headers.Length} колонок, разделитель «{DescribeDelimiter(delimiter)}».",
                $"Тип ключа: {(numeric ? "число" : "строка")}. Направление: {(options.Descending ? "по убыванию" : "по возрастанию")}.");

            if (options.Method == ExternalSortMethod.NaturalMerge)
            {
                foreach (var step in CreateNaturalRuns(inputPath, context))
                {
                    yield return step;
                }
            }
            else
            {
                foreach (var step in CreateFixedRuns(inputPath, context))
                {
                    yield return step;
                }
            }

            if (context.RecordsSelected == 0)
            {
                WriteEmptyOutput(outputPath, headers, delimiter);
                yield return new ExternalSortStep
                {
                    Kind = ExternalSortStepKind.Completed,
                    Title = "Подходящих записей нет — создан пустой файл с заголовком.",
                    LogText = $"Результат: {outputPath}",
                    Result = context.CreateResult(outputPath),
                };
                yield break;
            }

            var fanIn = options.Method == ExternalSortMethod.MultiwayMerge
                ? Math.Clamp(options.FanIn, 2, 8)
                : 2;

            while (context.Runs.Count > 1)
            {
                context.MergePasses++;
                var currentRuns = context.Runs.ToArray();
                var nextRuns = new List<string>();
                var pass = context.MergePasses;

                yield return Info(
                    context,
                    $"Проход слияния {pass}: объединяем {currentRuns.Length} серий по {fanIn} за раз.",
                    $"Серий до прохода: {currentRuns.Length}.");

                for (var start = 0; start < currentRuns.Length; start += fanIn)
                {
                    var group = currentRuns.Skip(start).Take(fanIn).ToArray();
                    if (group.Length == 1)
                    {
                        nextRuns.Add(group[0]);
                        continue;
                    }

                    var targetPath = Path.Combine(tempDirectory, $"merge_{pass}_{nextRuns.Count + 1:D3}.csv");
                    yield return new ExternalSortStep
                    {
                        Kind = ExternalSortStepKind.MergePass,
                        Title = $"Сливаем {group.Length} серии в один файл.",
                        LogText = $"Слияние серий: {string.Join(" + ", group.Select(Path.GetFileName))} → {Path.GetFileName(targetPath)}",
                        Comparisons = comparer.Comparisons,
                        Moves = context.Moves,
                        RunsCreated = context.RunsCreated,
                        MergePasses = pass,
                    };

                    foreach (var step in MergeGroup(group, targetPath, context))
                    {
                        yield return step;
                    }

                    nextRuns.Add(targetPath);
                }

                context.Runs.Clear();
                context.Runs.AddRange(nextRuns);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");
            File.Copy(context.Runs[0], outputPath, overwrite: true);

            var result = context.CreateResult(outputPath);
            yield return new ExternalSortStep
            {
                Kind = ExternalSortStepKind.Completed,
                Title = $"Готово! Отсортировано записей: {result.RecordCount}.",
                LogText = $"Результат сохранён: {outputPath}",
                Comparisons = result.Comparisons,
                Moves = result.Moves,
                RunsCreated = result.RunsCreated,
                MergePasses = result.MergePasses,
                Result = result,
            };
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static IEnumerable<ExternalSortStep> CreateFixedRuns(string inputPath, SortContext context)
    {
        using var reader = new CsvRecordReader(inputPath, context.Delimiter);
        var buffer = new List<CsvRecord>(context.Options.RunSize);

        while (true)
        {
            buffer.Clear();
            while (buffer.Count < context.Options.RunSize)
            {
                var row = reader.Read();
                if (row == null)
                {
                    break;
                }

                var record = new CsvRecord(row);
                if (!context.MatchesFilter(record))
                {
                    context.FilteredOut++;
                    continue;
                }

                context.RecordsSelected++;
                buffer.Add(record);
            }

            if (buffer.Count == 0)
            {
                break;
            }

            context.RunsCreated++;
            var runNumber = context.RunsCreated;
            yield return Info(
                context,
                $"Блок {runNumber}: прочитано {buffer.Count} записей. Сортируем блок в памяти.",
                $"Размер блока — {context.Options.RunSize} записей.");

            foreach (var step in SortChunk(buffer, context))
            {
                yield return step;
            }

            var runPath = Path.Combine(context.TempDirectory, $"run_{runNumber:D3}.csv");
            WriteRun(runPath, buffer, context);
            context.Runs.Add(runPath);

            yield return new ExternalSortStep
            {
                Kind = ExternalSortStepKind.RunCreated,
                Title = $"Серия {runNumber} записана во временный файл.",
                LogText = $"Серия {runNumber}: {buffer.Count} записей → {Path.GetFileName(runPath)}",
                Comparisons = context.Comparer.Comparisons,
                Moves = context.Moves,
                RunsCreated = context.RunsCreated,
                MergePasses = context.MergePasses,
            };

            if (buffer.Count < context.Options.RunSize)
            {
                break;
            }
        }
    }

    private static IEnumerable<ExternalSortStep> CreateNaturalRuns(string inputPath, SortContext context)
    {
        using var reader = new CsvRecordReader(inputPath, context.Delimiter);
        var currentRun = new List<CsvRecord>();

        while (true)
        {
            CsvRecord? record = null;
            while (reader.Read() is { } row)
            {
                var candidate = new CsvRecord(row);
                if (!context.MatchesFilter(candidate))
                {
                    context.FilteredOut++;
                    continue;
                }

                context.RecordsSelected++;
                record = candidate;
                break;
            }

            if (record == null)
            {
                break;
            }

            if (currentRun.Count > 0)
            {
                var comparison = context.Comparer.Compare(currentRun[^1], record);
                yield return ComparisonStep(context, currentRun[^1], record, comparison);

                if (comparison > 0)
                {
                    foreach (var step in FlushNaturalRun(currentRun, context))
                    {
                        yield return step;
                    }

                    currentRun.Clear();
                }
            }

            currentRun.Add(record);
        }

        if (currentRun.Count > 0)
        {
            foreach (var step in FlushNaturalRun(currentRun, context))
            {
                yield return step;
            }
        }
    }

    private static IEnumerable<ExternalSortStep> FlushNaturalRun(List<CsvRecord> records, SortContext context)
    {
        context.RunsCreated++;
        var runNumber = context.RunsCreated;
        var runPath = Path.Combine(context.TempDirectory, $"natural_{runNumber:D3}.csv");
        WriteRun(runPath, records, context);
        context.Runs.Add(runPath);

        yield return new ExternalSortStep
        {
            Kind = ExternalSortStepKind.RunCreated,
            Title = $"Найдена естественная серия {runNumber}: {records.Count} записей.",
            LogText = $"Естественная серия {runNumber}: {records.Count} записей → {Path.GetFileName(runPath)}",
            Comparisons = context.Comparer.Comparisons,
            Moves = context.Moves,
            RunsCreated = context.RunsCreated,
            MergePasses = context.MergePasses,
        };
    }

    /// <summary>
    /// Сортировка блока вставками. Именно здесь видно каждое сравнение и сдвиг,
    /// поэтому журнал полностью отражает работу алгоритма.
    /// </summary>
    private static IEnumerable<ExternalSortStep> SortChunk(List<CsvRecord> records, SortContext context)
    {
        for (var index = 1; index < records.Count; index++)
        {
            var current = records[index];
            var position = index - 1;

            while (position >= 0)
            {
                var comparison = context.Comparer.Compare(records[position], current);
                yield return ComparisonStep(context, records[position], current, comparison);

                if (comparison <= 0)
                {
                    break;
                }

                records[position + 1] = records[position];
                context.Moves++;
                yield return MoveStep(
                    context,
                    $"Сдвиг: {context.Comparer.Describe(records[position])} → позиция {position + 1}.",
                    $"Сдвиг внутри блока: {context.Comparer.Describe(records[position])} → позиция {position + 1}");
                position--;
            }

            records[position + 1] = current;
        }
    }

    private static IEnumerable<ExternalSortStep> MergeGroup(
        IReadOnlyList<string> runPaths,
        string targetPath,
        SortContext context)
    {
        var readers = new List<CsvRecordReader>();
        try
        {
            foreach (var runPath in runPaths)
            {
                readers.Add(new CsvRecordReader(runPath, context.Delimiter));
            }

            using var writer = new CsvRecordWriter(targetPath, context.Delimiter);
            writer.WriteHeader(context.Headers);

            var heads = new CsvRecord?[readers.Count];
            for (var index = 0; index < readers.Count; index++)
            {
                heads[index] = readers[index].Read() is { } row ? new CsvRecord(row) : null;
            }

            while (true)
            {
                var best = -1;
                for (var index = 0; index < heads.Length; index++)
                {
                    if (heads[index] is not { } candidate)
                    {
                        continue;
                    }

                    if (best < 0)
                    {
                        best = index;
                        continue;
                    }

                    var comparison = context.Comparer.Compare(candidate, heads[best]!);
                    yield return ComparisonStep(context, candidate, heads[best]!, comparison);

                    if (comparison < 0)
                    {
                        best = index;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                var selected = heads[best]!;
                writer.WriteRecord(selected.Fields);
                context.Moves++;

                yield return MoveStep(
                    context,
                    $"Записываем {context.Comparer.Describe(selected)} в «{Path.GetFileName(targetPath)}».",
                    $"Запись в файл «{Path.GetFileName(targetPath)}»: {context.Comparer.Describe(selected)}");

                heads[best] = readers[best].Read() is { } next ? new CsvRecord(next) : null;
            }
        }
        finally
        {
            foreach (var reader in readers)
            {
                reader.Dispose();
            }
        }
    }

    private static void WriteRun(string path, IReadOnlyList<CsvRecord> records, SortContext context)
    {
        using var writer = new CsvRecordWriter(path, context.Delimiter);
        writer.WriteHeader(context.Headers);
        foreach (var record in records)
        {
            writer.WriteRecord(record.Fields);
        }

        context.Moves += records.Count;
    }

    private static void WriteEmptyOutput(string outputPath, IReadOnlyList<string> headers, char delimiter)
    {
        using var writer = new CsvRecordWriter(outputPath, delimiter);
        writer.WriteHeader(headers);
    }

    private static bool IsNumericKey(string inputPath, char delimiter, int keyIndex)
    {
        using var reader = new CsvRecordReader(inputPath, delimiter);
        var hasValue = false;

        while (reader.Read() is { } row)
        {
            if (keyIndex >= row.Length)
            {
                return false;
            }

            var value = row[keyIndex];
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            hasValue = true;
            if (!ExternalRecordComparer.IsNumericValue(value))
            {
                return false;
            }
        }

        return hasValue;
    }

    private static ExternalSortStep Info(SortContext context, string title, string logText)
        => new()
        {
            Kind = ExternalSortStepKind.Info,
            Title = title,
            LogText = logText,
            Comparisons = context.Comparer.Comparisons,
            Moves = context.Moves,
            RunsCreated = context.RunsCreated,
            MergePasses = context.MergePasses,
        };

    private static ExternalSortStep ComparisonStep(
        SortContext context,
        CsvRecord left,
        CsvRecord right,
        int comparison)
    {
        var raw = context.Comparer.CompareKeys(left, right);
        var relation = raw < 0 ? "<" : raw > 0 ? ">" : "=";
        var direction = context.Options.Descending ? "убывание" : "возрастание";
        var decision = comparison <= 0 ? "левая запись идёт раньше" : "правая запись идёт раньше";

        return new ExternalSortStep
        {
            Kind = ExternalSortStepKind.Comparison,
            Title = "Сравниваем две записи.",
            LogText = $"Сравнение ({direction}): {context.Comparer.Describe(left)} {relation} {context.Comparer.Describe(right)} → {decision}",
            Comparisons = context.Comparer.Comparisons,
            Moves = context.Moves,
            RunsCreated = context.RunsCreated,
            MergePasses = context.MergePasses,
        };
    }

    private static ExternalSortStep MoveStep(SortContext context, string title, string logText)
        => new()
        {
            Kind = ExternalSortStepKind.Move,
            Title = title,
            LogText = logText,
            Comparisons = context.Comparer.Comparisons,
            Moves = context.Moves,
            RunsCreated = context.RunsCreated,
            MergePasses = context.MergePasses,
        };

    private static string DescribeDelimiter(char delimiter)
        => delimiter switch
        {
            '\t' => "табуляция",
            ';' => "точка с запятой",
            ',' => "запятая",
            _ => delimiter.ToString(),
        };

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Временные файлы не критичны: операционная система очистит их позже.
        }
        catch (UnauthorizedAccessException)
        {
            // См. комментарий выше.
        }
    }

    private sealed class SortContext(
        string[] headers,
        ExternalSortOptions options,
        char delimiter,
        ExternalRecordComparer comparer,
        string tempDirectory)
    {
        public string[] Headers { get; } = headers;

        public ExternalSortOptions Options { get; } = options;

        public char Delimiter { get; } = delimiter;

        public ExternalRecordComparer Comparer { get; } = comparer;

        public string TempDirectory { get; } = tempDirectory;

        public List<string> Runs { get; } = [];

        public long Moves { get; set; }

        public int RecordsSelected { get; set; }

        public int FilteredOut { get; set; }

        public int RunsCreated { get; set; }

        public int MergePasses { get; set; }

        public bool MatchesFilter(CsvRecord record)
        {
            if (Options.FilterColumnIndex < 0 || string.IsNullOrWhiteSpace(Options.FilterValue))
            {
                return true;
            }

            if (Options.FilterColumnIndex >= record.Fields.Length)
            {
                return false;
            }

            return string.Equals(
                record.Fields[Options.FilterColumnIndex].Trim(),
                Options.FilterValue.Trim(),
                StringComparison.CurrentCultureIgnoreCase);
        }

        public ExternalSortResult CreateResult(string outputPath)
            => new(
                outputPath,
                RecordsSelected,
                FilteredOut,
                Comparer.Comparisons,
                Moves,
                RunsCreated,
                MergePasses,
                Options.Method,
                Options.KeyColumnName,
                Options.Descending);
    }
}
