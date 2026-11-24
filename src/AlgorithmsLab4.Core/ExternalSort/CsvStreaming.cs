using System.Text;

namespace AlgorithmsLab4.Core.ExternalSort;

/// <summary>
/// Потоковый читатель CSV. Позволяет обрабатывать большие файлы, не загружая
/// их целиком в память — именно это требуется для внешней сортировки.
/// </summary>
internal sealed class CsvRecordReader : IDisposable
{
    private readonly StreamReader _reader;
    private readonly char _delimiter;

    public CsvRecordReader(string path, char delimiter)
    {
        _delimiter = delimiter;
        _reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        Headers = Read() ?? [];
    }

    public string[] Headers { get; }

    /// <summary>Читает следующую запись или null в конце файла.</summary>
    public string[]? Read()
    {
        string[]? fields;
        do
        {
            fields = ReadFields();
        }
        while (fields is { Length: 1 } && string.IsNullOrWhiteSpace(fields[0]));

        return fields;
    }

    public void Dispose() => _reader.Dispose();

    private string[]? ReadFields()
    {
        var fields = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;
        var hasData = false;

        while (true)
        {
            var next = _reader.Read();
            if (next < 0)
            {
                if (!hasData && fields.Count == 0 && builder.Length == 0)
                {
                    return null;
                }

                fields.Add(builder.ToString());
                return [.. fields];
            }

            var character = (char)next;
            if (inQuotes)
            {
                if (character == '"')
                {
                    if (_reader.Peek() == '"')
                    {
                        _reader.Read();
                        builder.Append('"');
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    builder.Append(character);
                }

                continue;
            }

            switch (character)
            {
                case '"':
                    inQuotes = true;
                    hasData = true;
                    break;
                case var value when value == _delimiter:
                    fields.Add(builder.ToString());
                    builder.Clear();
                    hasData = true;
                    break;
                case '\r':
                    if (_reader.Peek() == '\n')
                    {
                        _reader.Read();
                    }

                    fields.Add(builder.ToString());
                    return [.. fields];
                case '\n':
                    fields.Add(builder.ToString());
                    return [.. fields];
                default:
                    builder.Append(character);
                    hasData = true;
                    break;
            }
        }
    }
}

/// <summary>
/// Потоковый писатель CSV с корректным экранированием кавычек и разделителей.
/// </summary>
internal sealed class CsvRecordWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly char _delimiter;

    public CsvRecordWriter(string path, char delimiter)
    {
        _delimiter = delimiter;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public void WriteHeader(IReadOnlyList<string> headers) => WriteRecord(headers);

    public void WriteRecord(IReadOnlyList<string> fields)
    {
        for (var index = 0; index < fields.Count; index++)
        {
            if (index > 0)
            {
                _writer.Write(_delimiter);
            }

            _writer.Write(Escape(fields[index]));
        }

        _writer.WriteLine();
    }

    public void Dispose() => _writer.Dispose();

    private string Escape(string value)
    {
        if (value.IndexOfAny([_delimiter, '"', '\r', '\n']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
