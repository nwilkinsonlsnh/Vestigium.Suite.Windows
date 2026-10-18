using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Features;

public partial class ApplyFeatureChangesWindow : Window
{
    private readonly FeatureJob _job;

    public ApplyFeatureChangesWindow(FeatureJob job)
    {
        InitializeComponent();
        _job = job;
        Result = new FeatureResult { CorrelationId = job.CorrelationId, Cancelled = true };
        EnableList.ItemsSource = job.Enable.Select(Line).ToList();
        DisableList.ItemsSource = job.Disable.Select(Line).ToList();
        Summary.Text = "Nothing is changed until you confirm. Parents required by a checked feature, and children of a cleared feature, are already in these lists.";
        FeatureLog.Information(FeatureLog.Requested, $"Feature apply opened. enable={job.Enable.Count} disable={job.Disable.Count}", job.CorrelationId);
    }

    public FeatureResult Result { get; private set; }

    private static string Line(FeatureJobItem item)
    {
        var reason = string.IsNullOrEmpty(item.Reason) ? "" : $"  ({item.Reason})";
        var absent = item.IsAbsent ? "  [absent, may need a source]" : "";
        return $"{item.Caption}{reason}{absent}";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        FeatureLog.Information(FeatureLog.Cancelled, "Feature apply cancelled.", _job.CorrelationId);
        Result = new FeatureResult { CorrelationId = _job.CorrelationId, Cancelled = true };
        Close();
    }

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ConfirmButton.IsEnabled = false;
        var result = new FeatureResult { CorrelationId = _job.CorrelationId };
        var done = 0;
        var total = Math.Max(1, _job.Enable.Count + _job.Disable.Count);
        foreach (var item in _job.Enable)
        {
            Summary.Text = $"Enabling {item.Caption} ({++done} of {total})";
            var code = await RunAsync($"Enable-WindowsOptionalFeature -Online -FeatureName '{item.Name}' -NoRestart");
            Record(result, item, code, enabled: true);
        }
        foreach (var item in _job.Disable)
        {
            Summary.Text = $"Disabling {item.Caption} ({++done} of {total})";
            var code = await RunAsync($"Disable-WindowsOptionalFeature -Online -FeatureName '{item.Name}' -NoRestart");
            Record(result, item, code, enabled: false);
        }
        Result = result;
        FeatureLog.Information(
            FeatureLog.Completed,
            $"Feature apply completed. enabled={result.Enabled.Count} disabled={result.Disabled.Count} restart={result.Restart.Count} failed={result.Failed.Count}",
            _job.CorrelationId);
        Summary.Text = $"Enabled {result.Enabled.Count}. Disabled {result.Disabled.Count}. Restart required {result.Restart.Count}. Failed {result.Failed.Count}.";
        ConfirmButton.Content = "Close";
        ConfirmButton.IsEnabled = true;
        ConfirmButton.Click -= Confirm_Click;
        ConfirmButton.Click += (_, _) => Close();
    }

    private static void Record(FeatureResult result, FeatureJobItem item, int code, bool enabled)
    {
        if (code is 0 or 3010)
        {
            (enabled ? result.Enabled : result.Disabled).Add(item.Caption);
            FeatureLog.Information(FeatureLog.Changed, $"{(enabled ? "Enabled" : "Disabled")} {item.Name}. exit={code}", result.CorrelationId);
            if (code == 3010)
                result.Restart.Add(item.Caption);
            return;
        }
        result.Failed.Add($"{item.Caption} ({code})");
        FeatureLog.Information(FeatureLog.Changed, $"Failed {item.Name}. exit={code}", result.CorrelationId);
    }

    private static Task<int> RunAsync(string command)
    {
        return Task.Run(() =>
        {
            var start = new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -Command \"{command}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var process = Process.Start(start);
            process?.WaitForExit();
            return process?.ExitCode ?? -1;
        });
    }
}
