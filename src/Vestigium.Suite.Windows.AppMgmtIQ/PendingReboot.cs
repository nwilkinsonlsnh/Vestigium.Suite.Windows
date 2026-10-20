using Vestigium.Helpers.WinReg;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public sealed record PendingRebootState(bool Pending, IReadOnlyList<string> Renames)
{
    public static PendingRebootState Read()
    {
        var renames = new List<string>();
        var pending = false;
        try
        {
            var key = RegistryHelper.Local.GetKey(
                RegistryHiveKind.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\Session Manager",
                RegistryViewKind.Default,
                RegistryDetailLevel.Full);
            var value = key?.Values.FirstOrDefault(v => v.Name.Equals("PendingFileRenameOperations", StringComparison.OrdinalIgnoreCase));
            if (value is not null)
            {
                pending = true;
                renames.AddRange(Split(value.DataText));
            }
        }
        catch (Exception)
        {
        }

        pending |= KeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
        pending |= KeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
        return new PendingRebootState(pending, renames);
    }

    public bool Mentions(PackageRow row)
    {
        var needles = new[]
        {
            Text(row, "InstallLocation"),
            Text(row, "DisplayIcon"),
            Text(row, "UninstallString"),
            Text(row, "ModifyPath")
        }.Where(text => text.Length >= 4).ToArray();
        foreach (var rename in Renames)
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

    private static bool KeyExists(string path)
    {
        try
        {
            return RegistryHelper.Local.GetKey(RegistryHiveKind.LocalMachine, path, RegistryViewKind.Default, RegistryDetailLevel.Identity) is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static IEnumerable<string> Split(string? text)
        => (text ?? "").Split(['\r', '\n', '\0'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Text(PackageRow row, string name)
        => row.Values.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.DataText ?? "";
}
