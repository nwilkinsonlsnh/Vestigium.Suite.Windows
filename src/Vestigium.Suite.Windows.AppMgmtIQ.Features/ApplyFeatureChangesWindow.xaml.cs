using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Features;

public partial class ApplyFeatureChangesWindow : Window
{
    private readonly FeatureJob _job;
    private readonly List<FeatureOutcome> _outcomes = [];

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
        ButtonBar.Visibility = Visibility.Collapsed;
        PlanPage.Visibility = Visibility.Collapsed;
        ProgressPage.Visibility = Visibility.Visible;
        Progress.Maximum = Math.Max(1, _job.Enable.Count + _job.Disable.Count);
        var result = new FeatureResult { CorrelationId = _job.CorrelationId };
        var done = 0;
        var total = _job.Enable.Count + _job.Disable.Count;
        foreach (var item in _job.Enable)
        {
            ProgressText.Text = $"Enabling {item.Caption} ({++done} of {total})";
            Progress.Value = done;
            var run = await RunPowerShell($"Enable-WindowsOptionalFeature -Online -FeatureName '{item.Name}' -NoRestart -ErrorAction Stop");
            Record(result, item, run, enabled: true);
        }
        foreach (var item in _job.Disable)
        {
            ProgressText.Text = $"Disabling {item.Caption} ({++done} of {total})";
            Progress.Value = done;
            var run = await RunPowerShell($"Disable-WindowsOptionalFeature -Online -FeatureName '{item.Name}' -NoRestart -ErrorAction Stop");
            Record(result, item, run, enabled: false);
        }
        Result = result;
        FeatureLog.Information(
            FeatureLog.Completed,
            $"Feature apply completed. enabled={result.Enabled.Count} disabled={result.Disabled.Count} restart={result.Restart.Count} failed={result.Failed.Count}",
            _job.CorrelationId);
        ShowResults(result);
    }

    private void ShowResults(FeatureResult result)
    {
        var elevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        ResultSummary.Text = $"Enabled {result.Enabled.Count}    Disabled {result.Disabled.Count}    Restart required {result.Restart.Count}    Failed {result.Failed.Count}"
            + (elevated ? "" : "    This process is not elevated.");
        ResultList.ItemsSource = _outcomes.ToList();
        ProgressPage.Visibility = Visibility.Collapsed;
        ResultPage.Visibility = Visibility.Visible;
        ButtonBar.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Collapsed;
        ConfirmButton.Content = "Close";
        ConfirmButton.IsEnabled = true;
        ConfirmButton.Click -= Confirm_Click;
        ConfirmButton.Click += (_, _) => Close();
    }

    private void Record(FeatureResult result, FeatureJobItem item, DismRun run, bool enabled)
    {
        if (run.Code is 0 or 3010)
        {
            (enabled ? result.Enabled : result.Disabled).Add(item.Caption);
            var outcome = run.Code == 3010 ? "Restart required" : enabled ? "Enabled" : "Disabled";
            _outcomes.Add(new FeatureOutcome(item.Caption, outcome, run.Detail));
            FeatureLog.Information(FeatureLog.Changed, $"{outcome} {item.Name}. exit={run.Code}", result.CorrelationId);
            if (run.Code == 3010)
                result.Restart.Add(item.Caption);
            return;
        }
        var detail = string.IsNullOrWhiteSpace(run.Detail) ? $"Exit {run.Code}" : run.Detail;
        result.Failed.Add($"{item.Caption}: {detail}");
        _outcomes.Add(new FeatureOutcome(item.Caption, "Failed", detail));
        FeatureLog.Information(FeatureLog.Changed, $"Failed {item.Name}. exit={run.Code}. {detail}", result.CorrelationId);
    }

    private static Task<DismRun> RunPowerShell(string command)
    {
        return Task.Run(() =>
        {
            var start = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -Command \"" + command + "\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(start);
            if (process is null)
                return new DismRun(-1, "PowerShell did not start.");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            var detail = string.IsNullOrWhiteSpace(error) ? output.Trim() : error.Trim();
            return new DismRun(process.ExitCode, detail);
        });
    }

    private static string Detail(int code, string output, string error)
    {
        var text = string.IsNullOrWhiteSpace(error) ? output : error + Environment.NewLine + output;
        var line = text.Split('\n').Select(row => row.Trim()).FirstOrDefault(row => row.StartsWith("Error", StringComparison.OrdinalIgnoreCase));
        if (code == 50 && MiniNtPresent())
            return "Error 50. DISM thinks this machine is Windows PE because HKLM\\SYSTEM\\CurrentControlSet\\Control\\MiniNT exists. That key blocks /Online. It is not an elevation failure.";
        if (!string.IsNullOrWhiteSpace(line))
            return line;
        return code == 0 ? "" : $"Exit {code}";
    }

    private static bool MiniNtPresent()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\MiniNT");
        return key is not null;
    }

    private sealed record DismRun(int Code, string Detail);
    private sealed record FeatureOutcome(string Caption, string Outcome, string Detail);
}
