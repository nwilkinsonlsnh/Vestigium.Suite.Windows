using System.Diagnostics;
using System.Text;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Bloatware;

internal static class BloatwareApplier
{
    public static BloatwareResult Apply(BloatwareJob job)
    {
        var result = new BloatwareResult { CorrelationId = job.CorrelationId, Action = job.Action };
        BloatwareLog.Information(BloatwareLog.Requested, $"{job.Action} requested. {job.Family}", job.CorrelationId);
        var script = Script(job);
        if (script is null)
        {
            result.Error = "Unknown action.";
            return result;
        }
        var (code, output) = Run(script);
        result.Detail = output;
        if (code == 0)
        {
            result.Ok = true;
            BloatwareLog.Information(BloatwareLog.Changed, $"{job.Action} applied. {job.Family}", job.CorrelationId);
        }
        else
        {
            result.Error = string.IsNullOrWhiteSpace(output) ? $"PowerShell exited {code}." : output;
        }
        BloatwareLog.Information(BloatwareLog.Completed, result.Ok ? $"{job.Action} completed." : result.Error, job.CorrelationId);
        return result;
    }

    private static string? Script(BloatwareJob job)
    {
        var family = Quote(job.Family);
        var action = job.Action.Trim();
        if (action.Equals("Remove", StringComparison.OrdinalIgnoreCase))
            return "$ErrorActionPreference='Continue'; Get-AppxPackage -AllUsers -Name " + family + " | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -AllUsers }; Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -eq " + family + " } | Remove-AppxProvisionedPackage -Online";
        if (action.Equals("Add", StringComparison.OrdinalIgnoreCase))
            return "$ErrorActionPreference='Continue'; $registered=0; $sources=@(); Get-AppxPackage -AllUsers -Name " + family + " | ForEach-Object { $sources += $_.InstallLocation }; Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -eq " + family + " } | ForEach-Object { $sources += (Join-Path 'C:\\Program Files\\WindowsApps' $_.PackageName) }; $sources | Select-Object -Unique | ForEach-Object { $manifest = Join-Path $_ 'AppxManifest.xml'; if (Test-Path $manifest) { Add-AppxPackage -DisableDevelopmentMode -Register $manifest; $registered++ } }; if ($registered -eq 0) { throw 'No package source was found on this machine.' }";
        return null;
    }

    private static string Quote(string value) => "'" + (value ?? "").Replace("'", "''") + "'";

    private static (int Code, string Output) Run(string script)
    {
        var info = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -Command " + script)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = Process.Start(info);
        if (process is null)
            return (1, "PowerShell did not start.");
        var output = new StringBuilder();
        output.Append(process.StandardOutput.ReadToEnd());
        output.Append(process.StandardError.ReadToEnd());
        process.WaitForExit();
        return (process.ExitCode, output.ToString().Trim());
    }
}
