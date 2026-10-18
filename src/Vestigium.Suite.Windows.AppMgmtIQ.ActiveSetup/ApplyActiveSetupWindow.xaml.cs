using System.Windows;
using Microsoft.Win32;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ActiveSetup;

public partial class ApplyActiveSetupWindow : Window
{
    private readonly ActiveSetupJob _job;
    private bool _done;

    public ApplyActiveSetupWindow(ActiveSetupJob job)
    {
        _job = job;
        Result = new ActiveSetupResult
        {
            CorrelationId = job.CorrelationId,
            Action = job.Action,
            KeyPath = KeyPath(job)
        };
        InitializeComponent();
        Heading.Text = TitleFor(job.Action);
        ContinueButton.Content = ButtonFor(job.Action);
        CloseButton.Content = "Cancel";
        Summary.Text = Describe(job);
        PathText.Text = Result.KeyPath;
        CommandText.Text = string.IsNullOrWhiteSpace(job.StubPath) ? "No command." : job.StubPath;
        ActiveSetupLog.Information(ActiveSetupLog.Requested, $"{job.Action} requested. {Result.KeyPath}", job.CorrelationId);
    }

    public ActiveSetupResult Result { get; }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (_done)
        {
            Close();
            return;
        }

        var error = Apply();
        _done = true;
        if (error is null)
        {
            Result.Ok = true;
            ActiveSetupLog.Information(ActiveSetupLog.Changed, $"{_job.Action} applied. {Result.KeyPath}", _job.CorrelationId);
            ActiveSetupLog.Information(ActiveSetupLog.Completed, $"{_job.Action} completed.", _job.CorrelationId);
        }
        else
        {
            Result.Error = error;
            ActiveSetupLog.Information(ActiveSetupLog.Completed, $"{_job.Action} failed. {error}", _job.CorrelationId);
        }
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Result.Cancelled = true;
        ActiveSetupLog.Information(ActiveSetupLog.Cancelled, $"{_job.Action} cancelled.", _job.CorrelationId);
        Close();
    }

    private string? Apply()
    {
        var view = _job.IsX64 ? RegistryView.Registry64 : RegistryView.Registry32;
        var path = RelativePath(_job);
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            var action = _job.Action.Trim();
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
                if (!string.IsNullOrWhiteSpace(_job.DisplayName))
                    key.SetValue("", _job.DisplayName, RegistryValueKind.String);
                key.SetValue("StubPath", _job.StubPath ?? "", RegistryValueKind.String);
                key.SetValue("Version", string.IsNullOrWhiteSpace(_job.Version) ? "1,0" : _job.Version, RegistryValueKind.String);
                key.SetValue("IsInstalled", _job.Installed ? 1 : 0, RegistryValueKind.DWord);
                return null;
            }
            return "Unknown action.";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static string KeyPath(ActiveSetupJob job)
        => $@"HKLM\{(job.IsX64 ? "" : @"SOFTWARE\WOW6432Node\")}{(job.IsX64 ? @"SOFTWARE\" : "")}Microsoft\Active Setup\Installed Components\{job.Component}";

    private static string RelativePath(ActiveSetupJob job)
        => $@"{(job.IsX64 ? "SOFTWARE" : @"SOFTWARE\WOW6432Node")}\Microsoft\Active Setup\Installed Components\{job.Component}";

    private static string TitleFor(string action) => action.Trim().ToLowerInvariant() switch
    {
        "delete" => "Delete Active Setup Entry",
        "clearstubpath" => "Clear Active Setup Command",
        _ => "Add Active Setup Entry"
    };

    private static string ButtonFor(string action) => action.Trim().ToLowerInvariant() switch
    {
        "delete" => "Delete Entry",
        "clearstubpath" => "Clear Command",
        _ => "Add Entry"
    };

    private static string Describe(ActiveSetupJob job) => job.Action.Trim().ToLowerInvariant() switch
    {
        "delete" => $"Delete the machine entry {job.DisplayName}. This removes the key. New users will not run it.",
        "clearstubpath" => $"Clear the command on {job.DisplayName}. The key and version stay. It will not run.",
        _ => $"Add a machine entry named {job.DisplayName}. It will run at the next logon for a user who does not have it."
    };
}
