using DotNetFormatPlayground.Core;

namespace DotNetFormatPlayground.Tests;

public sealed class FormatEngineTests
{
    [Fact]
    public void BigIntegerPreservesEveryDigitAndGroupsByCulture()
    {
        var result = Format("1234567890123456789012345678901234567890", FormatValueType.BigInteger, "de-DE", "#,##0");

        Assert.True(result.Success);
        Assert.Equal("1234567890123456789012345678901234567890", result.ParsedValue);
        Assert.Equal("1.234.567.890.123.456.789.012.345.678.901.234.567.890", result.CustomResult);
    }

    [Fact]
    public void DecimalDoesNotRoundTripThroughBinaryFloatingPoint()
    {
        var result = Format("1234567.890123456789", FormatValueType.Decimal, "en-US", "0.##################");

        Assert.True(result.Success);
        Assert.Equal("1234567.890123456789", result.CustomResult);
        Assert.Contains("decimal.Parse", result.ToStringCode);
    }

    [Fact]
    public void CultureComparisonMakesSeparatorsVisible()
    {
        var result = Format("1234567.89", FormatValueType.Decimal, "fr-FR", "N2");
        var row = Assert.Single(result.StandardFormats, item => item.Format == "N");

        Assert.NotEqual(row.CultureResult, row.InvariantResult);
        Assert.Equal("1 234 567,89", result.CustomResult);
    }

    [Fact]
    public void DateTimeCustomFormatUsesCultureSpecificNames()
    {
        var result = Format("2026-11-26T14:30:15Z", FormatValueType.DateTime, "nl-BE", "dddd d MMMM yyyy");

        Assert.True(result.Success);
        Assert.Contains("november", result.CustomResult, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DateTime.Parse", result.InterpolationCode);
    }

    [Fact]
    public void InvalidCustomFormatIsReportedWithoutLosingParsedValue()
    {
        var result = Format("2.03:04:05", FormatValueType.TimeSpan, "en-US", "hh:mm");

        Assert.True(result.Success);
        Assert.NotEmpty(result.CustomError);
        Assert.Contains("Escape literal separators", result.CustomError);
        Assert.Equal("2.03:04:05", result.ParsedValue);
    }

    [Fact]
    public void GuidOffersAllFiveStandardRepresentations()
    {
        var result = Format("2f1c9e7b-37b5-4f29-a7d6-11de23cab987", FormatValueType.Guid, "en-US", "N");

        Assert.True(result.Success);
        Assert.Equal("2f1c9e7b37b54f29a7d611de23cab987", result.CustomResult);
        Assert.Equal(5, result.StandardFormats.Count);
    }

    [Fact]
    public void InvalidTypedInputReturnsActionableMessage()
    {
        var result = Format("12.5", FormatValueType.Int32, "en-US", "N0");

        Assert.False(result.Success);
        Assert.Contains("valid int", result.Error);
    }

    private static FormatResult Format(string input, FormatValueType type, string culture, string format) =>
        FormatEngine.Format(new(input, type, culture, format));
}
