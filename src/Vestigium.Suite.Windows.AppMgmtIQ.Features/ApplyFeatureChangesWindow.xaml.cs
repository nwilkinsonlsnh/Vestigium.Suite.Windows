using System.Diagnostics;
using System.Windows;

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
        var total = _job.Enable.Count + _job.Disable.Count;
        foreach (var item in _job.Enable)
        {
            Summary.Text = $"Enabling {item.Caption} ({++done} of {total})";
            var run = await RunDism($"/Online /Enable-Feature /FeatureName:{item.Name} /NoRestart /Quiet");
            Record(result, item, run, enabled: true);
        }
        foreach (var item in _job.Disable)
        {
            Summary.Text = $"Disabling {item.Caption} ({++done} of {total})";
            var run = await RunDism($"/Online /Disable-Feature /FeatureName:{item.Name} /NoRestart /Quiet");
            Record(result, item, run, enabled: false);
        }
        Result = result;
        FeatureLog.Information(
            FeatureLog.Completed,
            $"Feature apply completed. enabled={result.Enabled.Count} disabled={result.Disabled.Count} restart={result.Restart.Count} failed={result.Failed.Count}",
            _job.CorrelationId);
        Summary.Text = result.Failed.Count == 0
            ? $"Enabled {result.Enabled.Count}. Disabled {result.Disabled.Count}. Restart required {result.Restart.Count}."
            : $"Enabled {result.Enabled.Count}. Disabled {result.Disabled.Count}. Failed {result.Failed.Count}: {string.Join("; ", result.Failed)}";
        ConfirmButton.Content = "Close";
        ConfirmButton.IsEnabled = true;
        ConfirmButton.Click -= Confirm_Click;
        ConfirmButton.Click += (_, _) => Close();
    }

    private static void Record(FeatureResult result, FeatureJobItem item, DismRun run, bool enabled)
    {
        if (run.Code is 0 or 3010)
        {
            (enabled ? result.Enabled : result.Disabled).Add(item.Caption);
            FeatureLog.Information(FeatureLog.Changed, $"{(enabled ? "Enabled" : "Disabled")} {item.Name}. exit={run.Code}", result.CorrelationId);
            if (run.Code == 3010)
                result.Restart.Add(item.Caption);
            return;
        }
        var reason = string.IsNullOrWhiteSpace(run.Detail) ? $"exit {run.Code}" : run.Detail;
        result.Failed.Add($"{item.Caption}: {reason}");
        FeatureLog.Information(FeatureLog.Changed, $"Failed {item.Name}. exit={run.Code}. {reason}", result.CorrelationId);
    }

    private static Task<DismRun> RunDism(string arguments)
    {
        return Task.Run(() =>
        {
            var start = new ProcessStartInfo("dism.exe", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(start);
            if (process is null)
                return new DismRun(-1, "DISM did not start.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            var detail = FirstLine(string.IsNullOrWhiteSpace(error) ? output : error);
            return new DismRun(process.ExitCode, detail);
        });
    }

    private static string FirstLine(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0 && !trimmed.StartsWith("Deployment Image", StringComparison.OrdinalIgnoreCase))
                return trimmed;
        }
        return "";
    }

    private sealed record DismRun(int Code, string Detail);
}
