using System.IO;
using System.Text.Json;
using System.Windows;
using Vestigium.Logging;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ActiveSetup;

public partial class App : Application
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private void OnStartup(object sender, StartupEventArgs e)
    {
        ActiveSetupLog.Initialize();
        var jobPath = e.Args.Length == 0 ? "" : e.Args[0].Trim('"');
        ActiveSetupJob? job = null;
        try
        {
            if (jobPath.Length == 0 || !File.Exists(jobPath))
                throw new FileNotFoundException("The job file was not found.", jobPath);
            job = JsonSerializer.Deserialize<ActiveSetupJob>(File.ReadAllText(jobPath), Json) ?? new ActiveSetupJob();
            var themes = new ThemeManager();
            themes.RegisterSuiteV1();
            themes.Initialize(this, string.IsNullOrWhiteSpace(job.ThemeId) ? "LightBlue" : job.ThemeId);
            var window = new ApplyActiveSetupWindow(job);
            if (job.ParentWidth > 0 && job.ParentHeight > 0)
            {
                window.Left = job.ParentLeft + (job.ParentWidth - window.Width) / 2;
                window.Top = job.ParentTop + (job.ParentHeight - window.Height) / 2;
            }
            window.ShowDialog();
            WriteResult(jobPath, window.Result);
        }
        catch (Exception ex)
        {
            VestigiumLog.Write(ActiveSetupLog.Completed, VestigiumLogLevel.Error, VestigiumStatus.Failure, ActiveSetupLog.Category, "ActiveSetup", ex.Message, ex, ActiveSetupLog.AppId, job?.CorrelationId);
            WriteResult(jobPath, new ActiveSetupResult
            {
                CorrelationId = job?.CorrelationId ?? "",
                Action = job?.Action ?? "",
                Error = ex.ToString()
            });
        }
        Shutdown(0);
    }

    private static void WriteResult(string jobPath, ActiveSetupResult result)
    {
        if (jobPath.Length == 0)
            return;
        var resultPath = Path.ChangeExtension(jobPath, ".result.json");
        File.WriteAllText(resultPath, JsonSerializer.Serialize(result, Json));
    }
}
