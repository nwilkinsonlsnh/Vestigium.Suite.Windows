using System.Text.RegularExpressions;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public sealed record RepairPlan(string FileName, string Arguments, string Display)
{
    private static readonly Regex GuidPattern = new(@"\{[0-9A-Fa-f\-]{36}\}", RegexOptions.Compiled);

    public static RepairPlan? Build(PackageRow row)
    {
        if (row.NoRepair)
            return null;
        var code = ProductCode(row);
        if (code is null)
            return null;
        if (!row.WindowsInstaller && !LooksLikeInstaller(row))
            return null;
        var display = $"msiexec.exe /fa {code} /qb /norestart";
        return new RepairPlan("msiexec.exe", $"/fa {code} /qb /norestart", display);
    }

    private static bool LooksLikeInstaller(PackageRow row)
        => row.UninstallString.Contains("msiexec", StringComparison.OrdinalIgnoreCase);

    private static string? ProductCode(PackageRow row)
    {
        var name = row.KeyPath.Split('\\').LastOrDefault();
        if (name is not null && GuidPattern.IsMatch(name))
            return GuidPattern.Match(name).Value;
        var stored = row.Values.FirstOrDefault(v => v.Name.Equals("ProductCode", StringComparison.OrdinalIgnoreCase))?.DataText;
        if (!string.IsNullOrWhiteSpace(stored) && GuidPattern.IsMatch(stored))
            return GuidPattern.Match(stored).Value;
        return GuidPattern.IsMatch(row.UninstallString) ? GuidPattern.Match(row.UninstallString).Value : null;
    }
}
