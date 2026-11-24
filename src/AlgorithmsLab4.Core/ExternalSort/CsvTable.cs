using System.Text;

namespace AlgorithmsLab4.Core.ExternalSort;

/// <summary>
/// Простая CSV-таблица с произвольным количеством колонок. Поддерживаются
/// разделители «;», «,» и табуляция, кавычки и переводы строк внутри кавычек.
/// </summary>
public sealed class CsvTable
{
    private CsvTable(char delimiter, string[] headers, List<string[]> rows)
    {
        Delimiter = delimiter;
        Headers = headers;
        Rows = rows;
    }

    public char Delimiter { get; }

    public string[] Headers { get; }

    public IReadOnlyList<string[]> Rows { get; }

    public int ColumnCount => Headers.Length;

    /// <summary>
    /// Загружает таблицу из файла. Кодировка — UTF-8 (BOM определяется автоматически).
    /// </summary>
    public static CsvTable Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var delimiter = DetectDelimiter(path);
        using var reader = new CsvRecordReader(path, delimiter);
        var rows = new List<string[]>();

        while (reader.Read() is { } row)
        {
            rows.Add(row);
        }

        var columnCount = reader.Headers.Length;
        foreach (var row in rows)
        {
            columnCount = Math.Max(columnCount, row.Length);
        }

        columnCount = Math.Max(columnCount, 1);
        var headers = new string[columnCount];
        for (var column = 0; column < columnCount; column++)
        {
            headers[column] = column < reader.Headers.Length && !string.IsNullOrWhiteSpace(reader.Headers[column])
                ? reader.Headers[column].Trim()
                : $"Поле {column + 1}";
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.Length == columnCount)
            {
                continue;
            }

            Array.Resize(ref row, columnCount);
            for (var column = 0; column < columnCount; column++)
            {
                row[column] ??= string.Empty;
            }

            rows[rowIndex] = row;
        }

        return new CsvTable(delimiter, headers, rows);
    }

    /// <summary>
    /// Сохраняет таблицу в CSV-файл. Родительская папка создаётся автоматически.
    /// </summary>
    public void Save(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var writer = new CsvRecordWriter(path, Delimiter);
        writer.WriteHeader(Headers);
        foreach (var row in Rows)
        {
            writer.WriteRecord(row);
        }
    }

    /// <summary>
    /// Возвращает текстовое представление таблицы для предпросмотра в интерфейсе.
    /// </summary>
    public string ToPreviewText(int maxRows = 12)
    {
        var widths = new int[ColumnCount];
        for (var column = 0; column < ColumnCount; column++)
        {
            var width = Headers[column].Length;
            foreach (var row in Rows)
            {
                width = Math.Max(width, row[column].Length);
            }

            widths[column] = Math.Clamp(width, 5, 24);
        }

        var builder = new StringBuilder();
        AppendRow(builder, Headers, widths);
        builder.AppendLine(string.Join("─┼─", widths.Select(width => new string('─', width))));

        foreach (var row in Rows.Take(maxRows))
        {
            AppendRow(builder, row, widths);
        }

        if (Rows.Count > maxRows)
        {
            builder.AppendLine($"… ещё {Rows.Count - maxRows} строк(и)");
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Определяет разделитель по первой непустой строке файла.
    /// </summary>
    public static char DetectDelimiter(string path)
    {
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var line = reader.ReadLine();
        while (line != null && string.IsNullOrWhiteSpace(line))
        {
            line = reader.ReadLine();
        }

        if (line == null)
        {
            return ';';
        }

        var semicolons = 0;
        var commas = 0;
        var tabs = 0;
        var inQuotes = false;

        foreach (var character in line)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (inQuotes)
            {
                continue;
            }

            switch (character)
            {
                case ';':
                    semicolons++;
                    break;
                case ',':
                    commas++;
                    break;
                case '\t':
                    tabs++;
                    break;
            }
        }

        if (tabs > semicolons && tabs > commas)
        {
            return '\t';
        }

        return commas > semicolons ? ',' : ';';
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> fields, IReadOnlyList<int> widths)
    {
        for (var column = 0; column < widths.Count; column++)
        {
            if (column > 0)
            {
                builder.Append(" │ ");
            }

            var value = column < fields.Count ? fields[column].ReplaceLineEndings(" ") : string.Empty;
            if (value.Length > widths[column])
            {
                value = string.Concat(value.AsSpan(0, Math.Max(0, widths[column] - 1)), "…");
            }

            builder.Append(value.PadRight(widths[column]));
        }

        builder.AppendLine();
    }
}
