using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class StartupViewModel
{
    private void OpenSourceEntry()
    {
        if (Selected is not { } row)
            return;
        if (row.Source == "Startup folder")
        {
            var folder = row.Scope == "Common"
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
                : Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var path = Path.Combine(folder, row.Key);
            Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true });
            return;
        }

        var hive = row.Scope == "HKCU" ? "HKEY_CURRENT_USER" : "HKEY_LOCAL_MACHINE";
        var key = row.Source switch
        {
            "RunOnce" => row.Scope == "HKCU" ? UserRunOnce : RunOnce,
            "RunOnceEx" => RunOnceEx,
            _ => row.Scope == "HKCU" ? UserRun : Run
        };
        if (row.Bit == "32" && row.Scope == "HKLM")
            key = key.Replace("SOFTWARE\\Microsoft", "SOFTWARE\\WOW6432Node\\Microsoft");
        RegistryHelper.Local.SetValue(
            RegistryHiveKind.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit",
            "LastKey",
            "Computer\\" + hive + "\\" + key,
            RegistryValueKind.String,
            RegistryViewKind.Default,
            confirm: true);
        Process.Start(new ProcessStartInfo("regedit.exe") { UseShellExecute = true });
    }

    private void JumpToPackageEntry()
    {
        if (Selected is not { } row || string.IsNullOrWhiteSpace(row.Package) || _inventory is null)
            return;
        var package = _inventory.AllPackages.FirstOrDefault(item =>
            string.Equals(item.DisplayName, row.Package, StringComparison.OrdinalIgnoreCase));
        if (package is null)
        {
            Status = "The linked package is not in the inventory.";
            return;
        }

        var shell = App.Shell;
        if (shell is not null)
        {
            var item = shell["AppMgmtIQ"];
            if (item is not null)
                shell.SelectedItem = item;
        }
        CheckInventoryTab();
        _inventory.Select(package);
    }

    private static void CheckInventoryTab()
    {
        if (Application.Current.MainWindow is not { } window)
            return;
        foreach (var radio in Find<RadioButton>(window))
            if (radio.Content as string == "AppMgmtIQ")
                radio.IsChecked = true;
    }

    private static IEnumerable<T> Find<T>(DependencyObject start) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(start);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(start, i);
            if (child is T match)
                yield return match;
            foreach (var nested in Find<T>(child))
                yield return nested;
        }
    }
}
