using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class FeaturePlanWindow : Window
{
    private readonly IReadOnlyList<FeatureChange> _enables;
    private readonly IReadOnlyList<FeatureChange> _disables;

    public FeaturePlanWindow(IReadOnlyList<FeatureChange> enables, IReadOnlyList<FeatureChange> disables)
    {
        InitializeComponent();
        _enables = enables;
        _disables = disables;
        EnableList.ItemsSource = enables.Select(Line).ToList();
        DisableList.ItemsSource = disables.Select(Line).ToList();
        var elevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        ElevationNote.Text = elevated ? "" : "Run AppMgmtIQ as administrator to apply.";
        ConfirmButton.IsEnabled = elevated && (enables.Count > 0 || disables.Count > 0);
    }

    public List<string> Enabled { get; } = [];
    public List<string> Disabled { get; } = [];
    public List<string> Restart { get; } = [];
    public List<string> Failed { get; } = [];

    private static string Line(FeatureChange change)
    {
        var reason = string.IsNullOrEmpty(change.Reason) ? "" : $"  ({change.Reason})";
        var absent = change.IsAbsent ? "  [absent, may need a source]" : "";
        return $"{change.Caption}{reason}{absent}";
    }

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ConfirmButton.IsEnabled = false;
        var progress = new ProgressWindow(Math.Max(1, _enables.Count + _disables.Count)) { Owner = this };
        progress.Show();
        var done = 0;
        foreach (var change in _enables)
        {
            progress.Report($"Enabling {change.Caption}", done);
            var code = await RunAsync($"Enable-WindowsOptionalFeature -Online -FeatureName '{change.Name}' -NoRestart");
            Record(change, code, enabled: true);
            done++;
        }
        foreach (var change in _disables)
        {
            progress.Report($"Disabling {change.Caption}", done);
            var code = await RunAsync($"Disable-WindowsOptionalFeature -Online -FeatureName '{change.Name}' -NoRestart");
            Record(change, code, enabled: false);
            done++;
        }
        progress.Close();
        DialogResult = true;
    }

    private void Record(FeatureChange change, int code, bool enabled)
    {
        if (code is 0 or 3010)
        {
            (enabled ? Enabled : Disabled).Add(change.Caption);
            if (code == 3010)
                Restart.Add(change.Caption);
            return;
        }
        Failed.Add($"{change.Caption} ({code})");
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

    private sealed class ProgressWindow : Window
    {
        private readonly ProgressBar _bar = new() { Height = 8, Margin = new Thickness(0, 12, 0, 0) };
        private readonly TextBlock _line = new();

        public ProgressWindow(int total)
        {
            Title = "Applying features";
            Width = 460;
            Height = 160;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            _bar.Maximum = total;
            Content = new StackPanel { Margin = new Thickness(16), Children = { _line, _bar } };
        }

        public void Report(string text, int value)
        {
            _line.Text = text;
            _bar.Value = value;
        }
    }
}
