using System.Diagnostics;
using System.Windows;

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
        var result = new FeatureResult { CorrelationId = _job.CorrelationId };
        var done = 0;
        var total = _job.Enable.Count + _job.Disable.Count;
        foreach (var item in _job.Enable)
        {
            Summary.Text = $"Enabling {item.Caption} ({++done} of {total})";
            var run = await RunDism($"/Online /Enable-Feature /FeatureName:{item.Name} /NoRestart");
            Record(result, item, run, enabled: true);
        }
        foreach (var item in _job.Disable)
        {
            Summary.Text = $"Disabling {item.Caption} ({++done} of {total})";
            var run = await RunDism($"/Online /Disable-Feature /FeatureName:{item.Name} /NoRestart");
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
        ResultSummary.Text = $"Enabled {result.Enabled.Count}    Disabled {result.Disabled.Count}    Restart required {result.Restart.Count}    Failed {result.Failed.Count}";
        ResultList.ItemsSource = _outcomes.ToList();
        PlanPage.Visibility = Visibility.Collapsed;
        ResultPage.Visibility = Visibility.Visible;
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
            return new DismRun(process.ExitCode, Detail(process.ExitCode, output, error));
        });
    }

    private static string Detail(int code, string output, string error)
    {
        var text = string.IsNullOrWhiteSpace(error) ? output : error + Environment.NewLine + output;
        var line = text.Split('\n').Select(row => row.Trim()).FirstOrDefault(row => row.StartsWith("Error", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(line))
            return code == 50 ? line + " The request is not supported. The process may not be elevated." : line;
        return code == 0 ? "" : $"Exit {code}";
    }

    private sealed record DismRun(int Code, string Detail);
    private sealed record FeatureOutcome(string Caption, string Outcome, string Detail);
}
