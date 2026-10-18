using Microsoft.Win32;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ActiveSetup;

internal static class ActiveSetupApplier
{
    public static ActiveSetupResult Apply(ActiveSetupJob job)
    {
        var result = new ActiveSetupResult
        {
            CorrelationId = job.CorrelationId,
            Action = job.Action,
            KeyPath = KeyPath(job)
        };
        ActiveSetupLog.Information(ActiveSetupLog.Requested, $"{job.Action} requested. {result.KeyPath}", job.CorrelationId);
        var error = Run(job);
        if (error is null)
        {
            result.Ok = true;
            ActiveSetupLog.Information(ActiveSetupLog.Changed, $"{job.Action} applied. {result.KeyPath}", job.CorrelationId);
            ActiveSetupLog.Information(ActiveSetupLog.Completed, $"{job.Action} completed.", job.CorrelationId);
        }
        else
        {
            result.Error = error;
            ActiveSetupLog.Information(ActiveSetupLog.Completed, $"{job.Action} failed. {error}", job.CorrelationId);
        }
        return result;
    }

    private static string? Run(ActiveSetupJob job)
    {
        var view = job.IsX64 ? RegistryView.Registry64 : RegistryView.Registry32;
        var path = RelativePath(job);
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            var action = job.Action.Trim();
            if (action.Equals("Delete", StringComparison.OrdinalIgnoreCase))
            {
                root.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
                return null;
            }
            if (action.Equals("ClearStubPath", StringComparison.OrdinalIgnoreCase))
            {
                using var key = root.OpenSubKey(path, writable: true);
                if (key is null)
                    return "The key was not found.";
                key.DeleteValue("StubPath", throwOnMissingValue: false);
                return null;
            }
            if (action.Equals("Add", StringComparison.OrdinalIgnoreCase))
            {
                using var key = root.CreateSubKey(path, writable: true);
                if (key is null)
                    return "The key could not be created.";
                if (!string.IsNullOrWhiteSpace(job.DisplayName))
                    key.SetValue("", job.DisplayName, RegistryValueKind.String);
                key.SetValue("StubPath", job.StubPath ?? "", RegistryValueKind.String);
                key.SetValue("Version", string.IsNullOrWhiteSpace(job.Version) ? "1,0" : job.Version, RegistryValueKind.String);
                key.SetValue("IsInstalled", job.Installed ? 1 : 0, RegistryValueKind.DWord);
                return null;
            }
            return "Unknown action.";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }

    private static string KeyPath(ActiveSetupJob job)
        => $@"HKLM\{(job.IsX64 ? "SOFTWARE" : @"SOFTWARE\WOW6432Node")}\Microsoft\Active Setup\Installed Components\{job.Component}";

    private static string RelativePath(ActiveSetupJob job)
        => $@"{(job.IsX64 ? "SOFTWARE" : @"SOFTWARE\WOW6432Node")}\Microsoft\Active Setup\Installed Components\{job.Component}";
}
