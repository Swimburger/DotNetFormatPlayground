using System.Globalization;
using System.Numerics;

namespace DotNetFormatPlayground.Core;

public enum FormatValueType
{
    Int32,
    Int64,
    Int128,
    BigInteger,
    Decimal,
    Double,
    DateTime,
    DateTimeOffset,
    DateOnly,
    TimeOnly,
    TimeSpan,
    Guid
}

public sealed record FormatRequest(string Input, FormatValueType ValueType, string CultureName, string CustomFormat);
public sealed record FormatRow(string Format, string Description, string CultureResult, string InvariantResult);
public sealed record FormatToken(string Token, string Description, string Example);

public sealed record FormatResult
{
    public bool Success { get; init; }
    public string Error { get; init; } = string.Empty;
    public string ParsedValue { get; init; } = string.Empty;
    public string InputHint { get; init; } = string.Empty;
    public string CustomResult { get; init; } = string.Empty;
    public string CustomError { get; init; } = string.Empty;
    public IReadOnlyList<FormatRow> StandardFormats { get; init; } = [];
    public IReadOnlyList<FormatToken> Tokens { get; init; } = [];
    public string InterpolationCode { get; init; } = string.Empty;
    public string ToStringCode { get; init; } = string.Empty;
    public string StringFormatCode { get; init; } = string.Empty;
}

public static class FormatEngine
{
    public static FormatResult Format(FormatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
            return Failure("Enter a value to format.", request.ValueType);

        CultureInfo culture;
        try
        {
            culture = string.IsNullOrEmpty(request.CultureName)
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(request.CultureName);
        }
        catch (CultureNotFoundException exception)
        {
            return Failure(exception.Message, request.ValueType);
        }

        if (!TryParse(request, out var value, out var error))
            return Failure(error, request.ValueType);

        var formats = StandardsFor(request.ValueType);
        var rows = new List<FormatRow>();
        foreach (var item in formats)
        {
            rows.Add(new(item.Format, item.Description,
                SafeFormat(value!, item.Format, culture),
                SafeFormat(value!, item.Format, CultureInfo.InvariantCulture)));
        }

        var customResult = string.Empty;
        var customError = string.Empty;
        if (!string.IsNullOrEmpty(request.CustomFormat))
        {
            try { customResult = FormatValue(value!, request.CustomFormat, culture); }
            catch (FormatException exception) { customError = exception.Message; }
        }

        return new FormatResult
        {
            Success = true,
            ParsedValue = Canonical(value!, request.ValueType),
            InputHint = InputHint(request.ValueType),
            CustomResult = customResult,
            CustomError = customError,
            StandardFormats = rows,
            Tokens = TokensFor(request.ValueType),
            InterpolationCode = GenerateInterpolation(request, culture),
            ToStringCode = GenerateToString(request, culture),
            StringFormatCode = GenerateStringFormat(request, culture)
        };
    }

    public static string InputHint(FormatValueType type) => type switch
    {
        FormatValueType.Int32 => "Signed 32-bit integer, for example -1234567.",
        FormatValueType.Int64 => "Signed 64-bit integer, for example 9223372036854775807.",
        FormatValueType.Int128 => "Signed 128-bit integer; every digit is preserved.",
        FormatValueType.BigInteger => "Integer of any practical size; every digit is preserved.",
        FormatValueType.Decimal => "Base-10 decimal, ideal for money; up to 29 significant digits.",
        FormatValueType.Double => "IEEE 754 floating point, including scientific notation.",
        FormatValueType.DateTime => "ISO-style date and time, optionally ending in Z; for example 2026-11-26T14:30:15.1234567Z.",
        FormatValueType.DateTimeOffset => "ISO timestamp with an offset; for example 2026-11-26T14:30:15.1234567-05:00.",
        FormatValueType.DateOnly => "ISO calendar date, for example 2026-11-26.",
        FormatValueType.TimeOnly => "ISO time of day, for example 14:30:15.1234567.",
        FormatValueType.TimeSpan => "Invariant duration, for example 2.03:04:05.6789000.",
        FormatValueType.Guid => "A GUID in D, N, B, P, or X form.",
        _ => string.Empty
    };

    public static IReadOnlyList<FormatToken> TokensFor(FormatValueType type) => type switch
    {
        FormatValueType.Int32 or FormatValueType.Int64 or FormatValueType.Int128 or FormatValueType.BigInteger or FormatValueType.Decimal or FormatValueType.Double =>
        [
            new("0", "Required digit; emits zero when absent", "000000"),
            new("#", "Optional digit", "#,##0.##"),
            new(".", "Culture-specific decimal separator", "0.000"),
            new(",", "Group separator or scaling comma", "#,##0"),
            new("%", "Multiply by 100 and append percent symbol", "0.0%"),
            new("E+0", "Scientific exponent", "0.00E+00"),
            new(";", "Separate positive;negative;zero sections", "0.##;[negative] 0.##;zero"),
            new("'text'", "Insert literal text", "#,##0 'items'")
        ],
        FormatValueType.DateTime or FormatValueType.DateTimeOffset =>
        [
            new("yyyy", "Four-digit year", "yyyy-MM-dd"),
            new("MM", "Two-digit month", "MM/dd/yyyy"),
            new("MMM", "Abbreviated month name", "dd MMM yyyy"),
            new("dd", "Two-digit day", "yyyy-MM-dd"),
            new("HH", "24-hour clock hour", "HH:mm:ss"),
            new("hh", "12-hour clock hour", "hh:mm tt"),
            new("mm", "Two-digit minute", "HH:mm"),
            new("ss", "Two-digit second", "HH:mm:ss"),
            new("fffffff", "Seven fractional-second digits", "HH:mm:ss.fffffff"),
            new("K", "Time-zone information from DateTime Kind", "yyyy-MM-ddTHH:mm:ssK"),
            new("zzz", "Signed UTC offset", "yyyy-MM-dd HH:mm zzz"),
            new("'text'", "Insert literal text", "dddd 'at' HH:mm")
        ],
        FormatValueType.DateOnly =>
        [
            new("yyyy", "Four-digit year", "yyyy-MM-dd"),
            new("MM", "Two-digit month", "MM/dd/yyyy"),
            new("MMM", "Abbreviated month name", "dd MMM yyyy"),
            new("MMMM", "Full month name", "d MMMM yyyy"),
            new("dd", "Two-digit day", "yyyy-MM-dd"),
            new("ddd", "Abbreviated day name", "ddd, d MMM"),
            new("dddd", "Full day name", "dddd d MMMM yyyy")
        ],
        FormatValueType.TimeOnly =>
        [
            new("HH", "24-hour clock hour", "HH:mm:ss"),
            new("hh", "12-hour clock hour", "hh:mm tt"),
            new("mm", "Two-digit minute", "HH:mm"),
            new("ss", "Two-digit second", "HH:mm:ss"),
            new("fffffff", "Seven fractional-second digits", "HH:mm:ss.fffffff"),
            new("tt", "AM/PM designator", "hh:mm tt"),
            new("'text'", "Insert literal text", "HH 'hours' mm 'minutes'")
        ],
        FormatValueType.TimeSpan =>
        [
            new("d", "Days without leading zero", "d'.'hh':'mm':'ss"),
            new("dd", "Days with at least two digits", "dd'.'hh':'mm':'ss"),
            new("hh", "Hours from 00 through 23", "hh':'mm"),
            new("mm", "Minutes from 00 through 59", "hh':'mm':'ss"),
            new("ss", "Seconds from 00 through 59", "hh':'mm':'ss"),
            new("fffffff", "Seven fractional-second digits", "hh':'mm':'ss'.'fffffff"),
            new("\\", "Escape the next character", @"hh\:mm\:ss")
        ],
        FormatValueType.Guid =>
        [
            new("D", "32 digits separated by hyphens", "D"),
            new("N", "32 digits without separators", "N"),
            new("B", "Hyphenated digits enclosed in braces", "B"),
            new("P", "Hyphenated digits enclosed in parentheses", "P"),
            new("X", "Four hexadecimal values enclosed in braces", "X")
        ],
        _ => []
    };

    private static bool TryParse(FormatRequest request, out object? value, out string error)
    {
        var input = request.Input.Trim();
        var invariant = CultureInfo.InvariantCulture;
        var integerStyle = NumberStyles.AllowLeadingSign;
        var decimalStyle = NumberStyles.Float;
        var floatStyle = NumberStyles.Float;
        bool ok;
        value = request.ValueType switch
        {
            FormatValueType.Int32 => Try<int>(input, integerStyle, invariant, int.TryParse, out ok),
            FormatValueType.Int64 => Try<long>(input, integerStyle, invariant, long.TryParse, out ok),
            FormatValueType.Int128 => Try<Int128>(input, integerStyle, invariant, Int128.TryParse, out ok),
            FormatValueType.BigInteger => Try<BigInteger>(input, integerStyle, invariant, BigInteger.TryParse, out ok),
            FormatValueType.Decimal => Try<decimal>(input, decimalStyle, invariant, decimal.TryParse, out ok),
            FormatValueType.Double => Try<double>(input, floatStyle, invariant, double.TryParse, out ok),
            FormatValueType.DateTime => TryDateTime(input, out ok),
            FormatValueType.DateTimeOffset => TryDateTimeOffset(input, out ok),
            FormatValueType.DateOnly => TrySimple<DateOnly>(input, DateOnly.TryParse, out ok),
            FormatValueType.TimeOnly => TrySimple<TimeOnly>(input, TimeOnly.TryParse, out ok),
            FormatValueType.TimeSpan => TryTimeSpan(input, out ok),
            FormatValueType.Guid => Guid.TryParse(input, out var guid) ? Set(guid, out ok) : Set(default(Guid), out ok, false),
            _ => Set(input, out ok, false)
        };
        error = ok ? string.Empty : $"“{request.Input}” is not a valid {DisplayType(request.ValueType)} value. {InputHint(request.ValueType)}";
        return ok;
    }

    private delegate bool NumberParser<T>(string input, NumberStyles style, IFormatProvider provider, out T result);
    private static T Try<T>(string input, NumberStyles style, IFormatProvider provider, NumberParser<T> parser, out bool ok)
    {
        ok = parser(input, style, provider, out var value);
        return value;
    }

    private delegate bool SimpleParser<T>(string input, IFormatProvider? provider, DateTimeStyles styles, out T result);
    private static T TrySimple<T>(string input, SimpleParser<T> parser, out bool ok)
    {
        ok = parser(input, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var value);
        return value;
    }

    private static DateTime TryDateTime(string input, out bool ok)
    {
        ok = DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value);
        return value;
    }

    private static DateTimeOffset TryDateTimeOffset(string input, out bool ok)
    {
        ok = DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value);
        return value;
    }

    private static TimeSpan TryTimeSpan(string input, out bool ok)
    {
        ok = TimeSpan.TryParse(input, CultureInfo.InvariantCulture, out var value);
        return value;
    }

    private static T Set<T>(T value, out bool ok, bool state = true) { ok = state; return value; }

    private static IReadOnlyList<(string Format, string Description)> StandardsFor(FormatValueType type) => type switch
    {
        FormatValueType.Int32 or FormatValueType.Int64 or FormatValueType.Int128 or FormatValueType.BigInteger =>
            [("G", "General"), ("D", "Decimal digits"), ("N", "Number with groups"), ("X", "Hexadecimal")],
        FormatValueType.Decimal =>
            [("G", "General"), ("C", "Currency"), ("N", "Number"), ("F", "Fixed-point"), ("E", "Exponential"), ("P", "Percent")],
        FormatValueType.Double =>
            [("G", "General"), ("R", "Round-trip"), ("C", "Currency"), ("N", "Number"), ("F", "Fixed-point"), ("E", "Exponential"), ("P", "Percent")],
        FormatValueType.DateTime =>
            [("d", "Short date"), ("D", "Long date"), ("t", "Short time"), ("T", "Long time"), ("g", "General short"), ("G", "General long"), ("f", "Full date, short time"), ("F", "Full date, long time"), ("O", "Round-trip"), ("R", "RFC 1123"), ("s", "Sortable"), ("u", "Universal sortable")],
        FormatValueType.DateTimeOffset =>
            [("d", "Short date"), ("D", "Long date"), ("t", "Short time"), ("T", "Long time"), ("g", "General short"), ("G", "General long"), ("f", "Full date, short time"), ("F", "Full date, long time"), ("O", "Round-trip"), ("R", "RFC 1123")],
        FormatValueType.DateOnly =>
            [("d", "Short date"), ("D", "Long date"), ("M", "Month and day"), ("Y", "Year and month"), ("O", "Round-trip"), ("R", "RFC 1123")],
        FormatValueType.TimeOnly =>
            [("t", "Short time"), ("T", "Long time"), ("O", "Round-trip"), ("R", "RFC 1123")],
        FormatValueType.TimeSpan =>
            [("c", "Constant / invariant"), ("g", "General short"), ("G", "General long")],
        FormatValueType.Guid =>
            [("D", "Hyphenated"), ("N", "Compact"), ("B", "Braces"), ("P", "Parentheses"), ("X", "Hexadecimal initializer")],
        _ => []
    };

    private static string SafeFormat(object value, string format, CultureInfo culture)
    {
        try { return FormatValue(value, format, culture); }
        catch (FormatException) { return "Not supported by this type"; }
    }

    private static string FormatValue(object value, string format, CultureInfo culture) => value switch
    {
        IFormattable formattable => formattable.ToString(format, culture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    private static string Canonical(object value, FormatValueType type) => type switch
    {
        FormatValueType.DateTime => ((DateTime)value).ToString("O", CultureInfo.InvariantCulture),
        FormatValueType.DateTimeOffset => ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture),
        FormatValueType.DateOnly => ((DateOnly)value).ToString("O", CultureInfo.InvariantCulture),
        FormatValueType.TimeOnly => ((TimeOnly)value).ToString("O", CultureInfo.InvariantCulture),
        FormatValueType.TimeSpan => ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture),
        FormatValueType.Guid => ((Guid)value).ToString("D"),
        _ => FormatValue(value, "G", CultureInfo.InvariantCulture)
    };

    private static string DisplayType(FormatValueType type) => type switch
    {
        FormatValueType.Int32 => "int",
        FormatValueType.Int64 => "long",
        FormatValueType.Int128 => "Int128",
        _ => type.ToString()
    };

    private static string GenerateInterpolation(FormatRequest request, CultureInfo culture) => $$"""
        using System.Globalization;
        {{AdditionalUsing(request.ValueType)}}
        var culture = {{CultureExpression(culture)}};
        var value = {{ParseExpression(request)}};
        var text = value.ToString("{{Escape(request.CustomFormat)}}", culture);

        // Interpolation uses the current culture unless you create a FormattableString.
        FormattableString template = $"{value:{{Escape(request.CustomFormat)}}}";
        var interpolated = template.ToString(culture);
        """;

    private static string GenerateToString(FormatRequest request, CultureInfo culture) => $$"""
        using System.Globalization;
        {{AdditionalUsing(request.ValueType)}}
        var culture = {{CultureExpression(culture)}};
        var value = {{ParseExpression(request)}};
        var result = value.ToString("{{Escape(request.CustomFormat)}}", culture);
        """;

    private static string GenerateStringFormat(FormatRequest request, CultureInfo culture) => $$"""
        using System.Globalization;
        {{AdditionalUsing(request.ValueType)}}
        var culture = {{CultureExpression(culture)}};
        var value = {{ParseExpression(request)}};
        var result = string.Format(culture, "{0:{{EscapeBraces(request.CustomFormat)}}}", value);
        """;

    private static string ParseExpression(FormatRequest request)
    {
        var input = Escape(request.Input.Trim());
        return request.ValueType switch
        {
            FormatValueType.Int32 => $"int.Parse(\"{input}\", NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)",
            FormatValueType.Int64 => $"long.Parse(\"{input}\", NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)",
            FormatValueType.Int128 => $"Int128.Parse(\"{input}\", NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)",
            FormatValueType.BigInteger => $"BigInteger.Parse(\"{input}\", NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)",
            FormatValueType.Decimal => $"decimal.Parse(\"{input}\", NumberStyles.Float, CultureInfo.InvariantCulture)",
            FormatValueType.Double => $"double.Parse(\"{input}\", NumberStyles.Float, CultureInfo.InvariantCulture)",
            FormatValueType.DateTime => $"DateTime.Parse(\"{input}\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)",
            FormatValueType.DateTimeOffset => $"DateTimeOffset.Parse(\"{input}\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)",
            FormatValueType.DateOnly => $"DateOnly.Parse(\"{input}\", CultureInfo.InvariantCulture)",
            FormatValueType.TimeOnly => $"TimeOnly.Parse(\"{input}\", CultureInfo.InvariantCulture)",
            FormatValueType.TimeSpan => $"TimeSpan.Parse(\"{input}\", CultureInfo.InvariantCulture)",
            FormatValueType.Guid => $"Guid.Parse(\"{input}\")",
            _ => "default"
        };
    }

    private static string AdditionalUsing(FormatValueType type) => type == FormatValueType.BigInteger ? "using System.Numerics;" : string.Empty;
    private static string CultureExpression(CultureInfo culture) => culture.Equals(CultureInfo.InvariantCulture)
        ? "CultureInfo.InvariantCulture"
        : $"CultureInfo.GetCultureInfo(\"{Escape(culture.Name)}\")";
    private static string Escape(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
    private static string EscapeBraces(string value) => Escape(value).Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
    private static FormatResult Failure(string error, FormatValueType type) => new() { Error = error, InputHint = InputHint(type) };
}
