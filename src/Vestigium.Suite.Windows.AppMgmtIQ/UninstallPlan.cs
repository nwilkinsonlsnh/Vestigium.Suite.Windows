using System.Text.RegularExpressions;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public sealed record UninstallPlan(string FileName, string Arguments, string Display, bool Rewritten, string? Note)
{
    private static readonly Regex GuidPattern = new(@"\{[0-9A-Fa-f\-]{36}\}", RegexOptions.Compiled);

    public static UninstallPlan? Build(PackageRow row)
    {
        var raw = row.UninstallString?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var code = ProductCode(row, raw);
        if (code is not null && IsInstaller(row, raw))
        {
            var display = $"msiexec.exe /x {code} /qb";
            return new UninstallPlan(
                "msiexec.exe",
                $"/x {code} /qb",
                display,
                true,
                "Rewritten from the maintenance string. /I opens the installer dialog. /x removes the package.");
        }

        var (file, args) = Split(raw);
        return new UninstallPlan(file, args, raw, false, null);
    }

    private static bool IsInstaller(PackageRow row, string raw)
    {
        if (row.WindowsInstaller)
            return true;
        return raw.Contains("msiexec", StringComparison.OrdinalIgnoreCase)
            && raw.Contains("/I", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ProductCode(PackageRow row, string raw)
    {
        var name = row.KeyPath.Split('\\').LastOrDefault();
        if (name is not null && GuidPattern.IsMatch(name))
            return GuidPattern.Match(name).Value;
        var stored = row.Values.FirstOrDefault(v => v.Name.Equals("ProductCode", StringComparison.OrdinalIgnoreCase))?.DataText;
        if (!string.IsNullOrWhiteSpace(stored) && GuidPattern.IsMatch(stored))
            return GuidPattern.Match(stored).Value;
        return GuidPattern.IsMatch(raw) ? GuidPattern.Match(raw).Value : null;
    }

    private static (string File, string Args) Split(string command)
    {
        var text = command.Trim();
        if (text.StartsWith('"'))
        {
            var end = text.IndexOf('"', 1);
            if (end > 1)
                return (text[1..end], text[(end + 1)..].Trim());
        }

        var space = text.IndexOf(' ');
        return space < 0 ? (text, string.Empty) : (text[..space], text[(space + 1)..].Trim());
    }
}
