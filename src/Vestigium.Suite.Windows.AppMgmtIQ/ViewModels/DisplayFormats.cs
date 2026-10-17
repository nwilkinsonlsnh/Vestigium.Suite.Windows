using System.Globalization;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed record DateFormatOption(string Label, string Pattern);

public static class DisplayFormats
{
    public static IReadOnlyList<DateFormatOption> DateFormats { get; } =
    [
        new("DD MMM YYYY", "dd MMM yyyy"),
        new("YYYY-MM-DD", "yyyy-MM-dd"),
        new("MM/DD/YYYY", "MM/dd/yyyy"),
        new("DD/MM/YYYY", "dd/MM/yyyy"),
        new("MMM DD, YYYY", "MMM dd, yyyy"),
        new("YYYYMMDD", "yyyyMMdd")
    ];

    public static DateFormatOption DefaultDateFormat => DateFormats[0];

    public static string Date(string? raw, string pattern)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        var text = raw.Trim();
        if (text.Length == 8 &&
            DateTime.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            var formatted = date.ToString(pattern, CultureInfo.InvariantCulture);
            return pattern.Contains("MMM", StringComparison.Ordinal) ? formatted.ToUpperInvariant() : formatted;
        }

        return text;
    }

    public static string Size(string? raw, int decimals)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var kb) || kb < 0)
            return raw.Trim();

        var places = Math.Clamp(decimals, 0, 8);
        const double kbPerMb = 1024;
        const double kbPerGb = 1024 * 1024;

        if (kb >= kbPerGb)
            return Trim(kb / kbPerGb, places) + " GB";
        if (kb >= kbPerMb)
            return Trim(kb / kbPerMb, places) + " MB";
        return Trim(kb, places) + " KB";
    }

    private static string Trim(double value, int decimals)
    {
        var format = decimals == 0 ? "0" : "0." + new string('#', decimals);
        return value.ToString(format, CultureInfo.InvariantCulture);
    }
}
