using System.Globalization;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public static class StartupApproved
{
    public static bool IsEnabled(object? data)
    {
        if (data is byte[] bytes)
            return bytes.Length == 0 || bytes[0] is 0x02 or 0x06;
        if (data is string text && text.Length >= 2 &&
            byte.TryParse(text[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var first))
            return first is 0x02 or 0x06;
        return true;
    }
}
