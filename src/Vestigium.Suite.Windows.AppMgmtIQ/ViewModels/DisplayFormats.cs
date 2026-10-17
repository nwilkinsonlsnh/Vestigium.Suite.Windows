using System.Globalization;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public static class DisplayFormats
{
    public static string Date(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        var text = raw.Trim();
        if (text.Length == 8 &&
            DateTime.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

        return text;
    }

    public static string Size(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var kb) || kb < 0)
            return raw.Trim();

        const double kbPerMb = 1024;
        const double kbPerGb = 1024 * 1024;

        if (kb >= kbPerGb)
            return Trim(kb / kbPerGb) + " GB";
        if (kb >= kbPerMb)
            return Trim(kb / kbPerMb) + " MB";
        return Trim(kb) + " KB";
    }

    private static string Trim(double value)
    {
        var text = value.ToString("0.####", CultureInfo.InvariantCulture);
        return text;
    }
}
