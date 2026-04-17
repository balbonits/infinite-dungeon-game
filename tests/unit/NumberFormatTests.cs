using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Tests for <see cref="NumberFormat"/>. Spec: docs/inventory/items.md#number-display.
/// </summary>
public class NumberFormatTests
{
    // ── Abbrev: small numbers (no suffix) ────────────────────────────────────

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(99, "99")]
    [InlineData(100, "100")]
    [InlineData(999, "999")]
    public void Abbrev_Under1K_ShowsPlainNumber(long value, string expected)
    {
        NumberFormat.Abbrev(value).Should().Be(expected);
    }

    // ── Abbrev: K range (1K to <1M) ──────────────────────────────────────────

    [Theory]
    [InlineData(1_000, "1K")]
    [InlineData(1_234, "1.2K")]
    [InlineData(12_345, "12.3K")]
    [InlineData(99_900, "99.9K")]
    [InlineData(100_000, "100K")]
    [InlineData(345_678, "345K")]
    [InlineData(999_999, "999K")] // rounds down at the boundary
    public void Abbrev_K(long value, string expected)
    {
        NumberFormat.Abbrev(value).Should().Be(expected);
    }

    // ── Abbrev: M range (1M to <1B) ──────────────────────────────────────────

    [Theory]
    [InlineData(1_000_000, "1M")]
    [InlineData(1_234_567, "1.2M")]
    [InlineData(12_345_678, "12.3M")]
    [InlineData(123_456_789, "123M")]
    public void Abbrev_M(long value, string expected)
    {
        NumberFormat.Abbrev(value).Should().Be(expected);
    }

    // ── Abbrev: B, T, Qa, Qi ─────────────────────────────────────────────────

    [Theory]
    [InlineData(1_000_000_000L, "1B")]
    [InlineData(1_500_000_000L, "1.5B")]
    [InlineData(1_000_000_000_000L, "1T")]
    [InlineData(1_000_000_000_000_000L, "1Qa")]
    [InlineData(1_000_000_000_000_000_000L, "1Qi")]
    public void Abbrev_LargeSuffixes(long value, string expected)
    {
        NumberFormat.Abbrev(value).Should().Be(expected);
    }

    // ── Abbrev: negative numbers ────────────────────────────────────────────

    [Fact]
    public void Abbrev_Negative_PreservesSign()
    {
        NumberFormat.Abbrev(-1_234).Should().Be("-1.2K");
        NumberFormat.Abbrev(-42).Should().Be("-42");
    }

    // ── Abbrev: long.MaxValue sanity ────────────────────────────────────────

    [Fact]
    public void Abbrev_LongMaxValue_UsesQiSuffix()
    {
        // long.MaxValue = 9,223,372,036,854,775,807 — should render cleanly as Qi
        string result = NumberFormat.Abbrev(long.MaxValue);
        result.Should().EndWith("Qi");
        result.Should().StartWith("9");
    }

    // ── Abbrev: truncation (not rounding) ───────────────────────────────────

    [Theory]
    [InlineData(1_950L, "1.9K")]   // rounding would lift to 2.0K — we truncate to 1.9K
    [InlineData(1_999L, "1.9K")]
    [InlineData(12_399L, "12.3K")] // rounding would lift to 12.4K
    [InlineData(99_999L, "99.9K")] // rounding would lift to 100.0K (ugly seam at the 100K boundary)
    [InlineData(1_999_999L, "1.9M")]
    [InlineData(99_999_999L, "99.9M")]
    public void Abbrev_UnderHundredRange_TruncatesInsteadOfRounding(long value, string expected)
    {
        NumberFormat.Abbrev(value).Should().Be(expected);
    }

    [Fact]
    public void Abbrev_LongMinValue_HandledWithoutOverflow()
    {
        // long.MinValue cannot be negated within `long` range (−long.MinValue wraps back
        // to itself), so naive recursion via Abbrev(-value) would stack-overflow.
        // Verify it's special-cased and returns a sensible string.
        string result = NumberFormat.Abbrev(long.MinValue);
        result.Should().StartWith("-");
        result.Should().EndWith("Qi");
    }

    // ── Full: comma-separated ────────────────────────────────────────────────

    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(1_000, "1,000")]
    [InlineData(1_234_567, "1,234,567")]
    [InlineData(-1_234, "-1,234")]
    public void Full_CommaSeparated(long value, string expected)
    {
        NumberFormat.Full(value).Should().Be(expected);
    }
}
