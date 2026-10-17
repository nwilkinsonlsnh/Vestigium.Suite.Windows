using System.Text.RegularExpressions;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public enum UninstallMode
{
    Registered,
    QuietNone,
    QuietBasic
}

public sealed record UninstallPlan(string FileName, string Arguments, string Display, bool Rewritten, string? Note)
{
    private static readonly Regex GuidPattern = new(@"\{[0-9A-Fa-f\-]{36}\}", RegexOptions.Compiled);

    public static bool CanForce(PackageRow row)
    {
        var raw = row.UninstallString?.Trim();
        return !string.IsNullOrWhiteSpace(raw) && ProductCode(row, raw) is not null && IsInstaller(row, raw);
    }

    public static UninstallPlan? Build(PackageRow row, UninstallMode mode)
    {
        var raw = row.UninstallString?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (mode == UninstallMode.Registered)
        {
            var (file, args) = Split(raw);
            return new UninstallPlan(file, args, raw, false, null);
        }

        var code = ProductCode(row, raw);
        if (code is null || !IsInstaller(row, raw))
            return null;

        var ui = mode == UninstallMode.QuietNone ? "/qn" : "/qb";
        var label = mode == UninstallMode.QuietNone ? "no interface" : "basic interface";
        var display = $"msiexec.exe /x {code} {ui} /norestart";
        return new UninstallPlan("msiexec.exe", $"/x {code} {ui} /norestart", display, true, $"Forced uninstall, {label}.");
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
