using System.Diagnostics;
using System.Text;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Bloatware;

internal static class BloatwareApplier
{
    public static BloatwareResult Apply(BloatwareJob job)
    {
        var result = new BloatwareResult { CorrelationId = job.CorrelationId, Action = job.Action };
        BloatwareLog.Information(BloatwareLog.Requested, $"{job.Action} requested. {job.Family}", job.CorrelationId);
        if (job.Action.Trim().Equals("Add", StringComparison.OrdinalIgnoreCase))
            return Add(job, result);
        var script = RemoveScript(job);
        if (script is null)
        {
            result.Error = "Unknown action.";
            return result;
        }
        return Finish(job, result, Run(script));
    }

    private static BloatwareResult Add(BloatwareJob job, BloatwareResult result)
    {
        var local = Run(RegisterScript(job.Family));
        if (local.Code == 0)
        {
            result.Detail = "Registered from the local package.";
            return Finish(job, result, local);
        }
        var fetched = Run(WingetScript(job.Name, job.Family));
        if (fetched.Code == 0)
        {
            result.Detail = "Fetched with winget.";
            return Finish(job, result, fetched);
        }
        result.OpenStore = false;
        result.Error = Retired(job.Family)
            ? $"{job.Name} is no longer available. Microsoft retired this package, and it is not offered in the Microsoft Store."
            : $"{job.Name} is not on this machine, and it could not be fetched. There is no local copy, and the Microsoft Store source did not have it.";
        result.Detail = fetched.Output;
        BloatwareLog.Information(BloatwareLog.Completed, result.Error, job.CorrelationId);
        return result;
    }

    private static BloatwareResult Finish(BloatwareJob job, BloatwareResult result, (int Code, string Output) run)
    {
        result.Detail = string.IsNullOrWhiteSpace(result.Detail) ? run.Output : result.Detail;
        if (run.Code == 0)
        {
            result.Ok = true;
            BloatwareLog.Information(BloatwareLog.Changed, $"{job.Action} applied. {job.Family}", job.CorrelationId);
        }
        else
        {
            result.Error = string.IsNullOrWhiteSpace(run.Output) ? $"The command exited {run.Code}." : run.Output;
        }
        BloatwareLog.Information(BloatwareLog.Completed, result.Ok ? $"{job.Action} completed." : result.Error, job.CorrelationId);
        return result;
    }

    private static string? RemoveScript(BloatwareJob job)
    {
        if (!job.Action.Trim().Equals("Remove", StringComparison.OrdinalIgnoreCase))
            return null;
        var family = Quote(job.Family);
        return "$ErrorActionPreference='Continue'; Get-AppxPackage -AllUsers -Name " + family + " | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -AllUsers }; Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -eq " + family + " } | Remove-AppxProvisionedPackage -Online";
    }

    private static string RegisterScript(string family)
    {
        var quoted = Quote(family);
        return "$ErrorActionPreference='Stop'; $registered=0; $sources=@(); Get-AppxPackage -AllUsers -Name " + quoted + " | ForEach-Object { $sources += $_.InstallLocation }; Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -eq " + quoted + " } | ForEach-Object { $sources += (Join-Path 'C:\\Program Files\\WindowsApps' $_.PackageName) }; $sources | Select-Object -Unique | ForEach-Object { $manifest = Join-Path $_ 'AppxManifest.xml'; if (Test-Path $manifest) { Add-AppxPackage -DisableDevelopmentMode -Register $manifest; $registered++ } }; if ($registered -eq 0) { throw 'No local manifest.' }";
    }

    private static bool Retired(string family) => family.ToLowerInvariant() is
        "microsoft.mspaint" or "microsoft.print3d" or "microsoft.3dbuilder" or "microsoft.microsoft3dviewer"
        or "microsoft.wallet" or "microsoft.oneconnect" or "microsoft.mixedreality.portal"
        or "microsoft.549981c3f5f10" or "microsoft.windows.devhome";

    private static string WingetScript(string name, string family)
    {
        var display = Quote(name);
        var package = Quote(family);
        return "$ErrorActionPreference='Continue'; $queries=@(" + display + "," + package + "); $installed=$false; foreach ($q in $queries) { $rows = winget search --query $q --accept-source-agreements | Where-Object { $_ -match '\\s(winget|msstore)\\s*$' }; $pick = $rows | Where-Object { $_ -notmatch 'classic' } | Select-Object -First 1; if (-not $pick) { $pick = $rows | Select-Object -First 1 }; if (-not $pick) { continue }; $parts = ($pick.Trim() -split '\\s+'); $source = $parts[-1]; $id = $parts[-2]; Write-Output ('Installing ' + $id + ' from ' + $source); winget install --id $id --source $source --accept-package-agreements --accept-source-agreements --disable-interactivity; if ($LASTEXITCODE -eq 0) { $installed=$true; break } }; if (-not $installed) { throw 'winget could not install a matching package.' }";
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
