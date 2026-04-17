using System;
using System.Globalization;

namespace DungeonGame;

/// <summary>
/// Abbreviated display for large numbers (item stacks, gold). See docs/inventory/items.md#number-display.
///
/// Rules:
/// - 0 – 999        → plain ("0" – "999")
/// - 1,000 – 999K   → "1.2K" – "999K" (1 decimal under 100K, 0 decimals at/above)
/// - 10^6 – 10^9    → "M"
/// - 10^9 – 10^12   → "B"
/// - 10^12 – 10^15  → "T"
/// - 10^15 – 10^18  → "Qa"
/// - 10^18+         → "Qi"
///
/// Tooltip callers should use <see cref="Full"/> for the exact value.
/// </summary>
public static class NumberFormat
{
    /// <summary>Abbreviated display ("12.3K", "45M", etc.). Negative values preserve the sign.</summary>
    public static string Abbrev(long value)
    {
        // long.MinValue has no positive counterpart in long — -(-long.MinValue) wraps back
        // to itself, so naive recursion via Abbrev(-value) infinite-loops. Special-case it.
        if (value == long.MinValue) return "-9.2Qi";
        if (value < 0) return "-" + Abbrev(-value);
        if (value < 1_000) return value.ToString(CultureInfo.InvariantCulture);

        (double scaled, string suffix) = value switch
        {
            < 1_000_000L => (value / 1_000d, "K"),
            < 1_000_000_000L => (value / 1_000_000d, "M"),
            < 1_000_000_000_000L => (value / 1_000_000_000d, "B"),
            < 1_000_000_000_000_000L => (value / 1_000_000_000_000d, "T"),
            < 1_000_000_000_000_000_000L => (value / 1_000_000_000_000_000d, "Qa"),
            _ => (value / 1_000_000_000_000_000_000d, "Qi"),
        };

        // 1 decimal under 100 (e.g., "1.2K", "99.9M"), 0 decimals at/above (e.g., "123K", "456M").
        // Truncate (floor) rather than round so the 1-decimal range stays within 1.0–99.9 —
        // rounding would lift 1_950 → "2.0K" and 99_950 → "100.0K", both of which look wrong
        // (the "100.0K" form is especially ugly next to the "100K" it transitions into).
        string formatted;
        if (scaled < 100)
        {
            double truncated = Math.Floor(scaled * 10d) / 10d;
            formatted = truncated.ToString("0.#", CultureInfo.InvariantCulture);
        }
        else
        {
            formatted = ((long)scaled).ToString(CultureInfo.InvariantCulture);
        }

        return formatted + suffix;
    }

    /// <summary>Full comma-separated display ("1,234,567"). Use in tooltips.</summary>
    public static string Full(long value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);
}
