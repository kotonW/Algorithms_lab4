using System.Globalization;

namespace AlgorithmsLab4.Core.ExternalSort;

internal sealed record CsvRecord(string[] Fields);

/// <summary>
/// Сравнивает записи по выбранной колонке. Если все значения колонки похожи
/// на числа, используется числовое сравнение, иначе — строковое (ru-RU).
/// </summary>
internal sealed class ExternalRecordComparer
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly StringComparer _stringComparer = StringComparer.Create(RussianCulture, ignoreCase: true);
    private readonly int _keyIndex;
    private readonly bool _numeric;
    private readonly bool _descending;
    private readonly string _keyName;

    public ExternalRecordComparer(int keyIndex, string keyName, bool numeric, bool descending)
    {
        _keyIndex = keyIndex;
        _keyName = keyName;
        _numeric = numeric;
        _descending = descending;
    }

    public long Comparisons { get; private set; }

    public bool Descending => _descending;

    public int Compare(CsvRecord left, CsvRecord right)
    {
        var result = CompareKeys(left, right);
        Comparisons++;
        return _descending ? -result : result;
    }

    /// <summary>Сравнение без увеличения счётчика — нужно для красивой строки журнала.</summary>
    public int CompareKeys(CsvRecord left, CsvRecord right)
    {
        var leftValue = KeyOf(left);
        var rightValue = KeyOf(right);

        if (_numeric && TryParseNumber(leftValue, out var leftNumber) && TryParseNumber(rightValue, out var rightNumber))
        {
            return leftNumber.CompareTo(rightNumber);
        }

        return _stringComparer.Compare(leftValue, rightValue);
    }

    public string KeyOf(CsvRecord record)
        => _keyIndex < record.Fields.Length ? record.Fields[_keyIndex].Trim() : string.Empty;

    public string Describe(CsvRecord record)
    {
        var label = record.Fields.Length > 0 ? record.Fields[0].Trim() : string.Empty;
        var key = KeyOf(record);

        if (string.Equals(label, key, StringComparison.Ordinal))
        {
            return $"«{label}»";
        }

        return $"«{label}» ({_keyName} = {key})";
    }

    public static bool IsNumericValue(string value) => TryParseNumber(value, out _);

    private static bool TryParseNumber(string value, out decimal result)
    {
        var normalized = value
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace("\u202F", string.Empty, StringComparison.Ordinal);

        if (normalized.Length == 0)
        {
            result = 0;
            return false;
        }

        var hasComma = normalized.Contains(',', StringComparison.Ordinal);
        var hasDot = normalized.Contains('.', StringComparison.Ordinal);
        if (hasComma && hasDot)
        {
            normalized = normalized.Replace(",", string.Empty, StringComparison.Ordinal);
        }
        else if (hasComma)
        {
            normalized = normalized.Replace(',', '.');
        }
        else if (normalized.Count(character => character == '.') > 1)
        {
            normalized = normalized.Replace(".", string.Empty, StringComparison.Ordinal);
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.Number | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture,
            out result);
    }
}
