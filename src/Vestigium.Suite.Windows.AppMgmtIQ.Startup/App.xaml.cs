using System.IO;
using System.Text.Json;
using System.Windows;
using Vestigium.Logging;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Startup;

public partial class App : Application
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private void OnStartup(object sender, StartupEventArgs e)
    {
        StartupLog.Initialize();
        var jobPath = e.Args.Length == 0 ? "" : e.Args[0].Trim('"');
        StartupJob? job = null;
        try
        {
            if (jobPath.Length == 0 || !File.Exists(jobPath))
                throw new FileNotFoundException("The job file was not found.", jobPath);
            job = JsonSerializer.Deserialize<StartupJob>(File.ReadAllText(jobPath), Json) ?? new StartupJob();
            WriteResult(jobPath, StartupApplier.Apply(job));
        }
        catch (Exception ex)
        {
            VestigiumLog.Write(StartupLog.Completed, VestigiumLogLevel.Error, VestigiumStatus.Failed, StartupLog.Category, "Startup", ex.Message, ex, StartupLog.AppId, job?.CorrelationId);
            WriteResult(jobPath, new StartupResult { CorrelationId = job?.CorrelationId ?? "" });
        }
        Shutdown(0);
    }

    private static void WriteResult(string jobPath, StartupResult result)
    {
        if (jobPath.Length == 0)
            return;
        File.WriteAllText(Path.ChangeExtension(jobPath, ".result.json"), JsonSerializer.Serialize(result, Json));
    }
}
