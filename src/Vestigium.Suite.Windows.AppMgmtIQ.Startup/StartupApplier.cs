using Microsoft.Win32;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Startup;

internal static class StartupApplier
{
    private const string Approved = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved";

    public static StartupResult Apply(StartupJob job)
    {
        var result = new StartupResult { CorrelationId = job.CorrelationId };
        StartupLog.Information(StartupLog.Requested, $"Startup change requested. {job.Items.Count} items.", job.CorrelationId);
        foreach (var item in job.Items)
            Apply(item, result);
        StartupLog.Information(StartupLog.Completed, $"Startup change completed. changed={result.Changed.Count} denied={result.Denied.Count} failed={result.Failed.Count}", job.CorrelationId);
        return result;
    }

    private static void Apply(StartupJobItem item, StartupResult result)
    {
        var name = string.IsNullOrWhiteSpace(item.Name) ? item.Key : item.Name;
        var branch = item.Branch is "Run" or "Run32" or "StartupFolder" ? item.Branch : "Run";
        var view = item.IsX64 ? RegistryView.Registry64 : RegistryView.Registry32;
        var data = item.Action.Equals("Enable", StringComparison.OrdinalIgnoreCase) ? Enabled() : Disabled();
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var key = root.CreateSubKey(Approved + "\\" + branch, writable: true);
            if (key is null)
            {
                result.Denied.Add(name);
                return;
            }
            key.SetValue(item.Key, data, RegistryValueKind.Binary);
            result.Changed.Add(name);
            StartupLog.Information(StartupLog.Changed, $"{item.Action} {name} ({branch}).", result.CorrelationId);
        }
        catch (UnauthorizedAccessException)
        {
            result.Denied.Add(name);
        }
        catch (Exception)
        {
            result.Failed.Add(name);
        }
    }

    private static byte[] Enabled() => [0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    private static byte[] Disabled() => [0x03, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
}
