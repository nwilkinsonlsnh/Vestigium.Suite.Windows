using Vestigium.Helpers.SystemInfo;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public static class PendingReboot
{
    public static bool Mentions(PendingRebootState state, PackageRow row)
    {
        var needles = new[]
        {
            Text(row, "InstallLocation"),
            Text(row, "DisplayIcon"),
            Text(row, "UninstallString"),
            Text(row, "ModifyPath")
        }.Where(text => text.Length >= 4).ToArray();
        foreach (var rename in state.Renames)
        {
            var path = rename.Replace(@"\??\", "", StringComparison.OrdinalIgnoreCase);
            foreach (var needle in needles)
            {
                if (path.Contains(needle, StringComparison.OrdinalIgnoreCase) || needle.Contains(path, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private static string Text(PackageRow row, string name)
        => row.Values.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.DataText ?? "";
}
