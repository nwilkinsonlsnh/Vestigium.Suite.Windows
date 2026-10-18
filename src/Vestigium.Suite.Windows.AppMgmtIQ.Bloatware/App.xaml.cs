using System.IO;
using System.Text.Json;
using System.Windows;
using Vestigium.Logging;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Bloatware;

public partial class App : Application
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private void OnStartup(object sender, StartupEventArgs e)
    {
        BloatwareLog.Initialize();
        var jobPath = e.Args.Length == 0 ? "" : e.Args[0].Trim('"');
        BloatwareJob? job = null;
        try
        {
            if (jobPath.Length == 0 || !File.Exists(jobPath))
                throw new FileNotFoundException("The job file was not found.", jobPath);
            job = JsonSerializer.Deserialize<BloatwareJob>(File.ReadAllText(jobPath), Json) ?? new BloatwareJob();
            WriteResult(jobPath, BloatwareApplier.Apply(job));
        }
        catch (Exception ex)
        {
            VestigiumLog.Write(BloatwareLog.Completed, VestigiumLogLevel.Error, VestigiumStatus.Failed, BloatwareLog.Category, "Bloatware", ex.Message, ex, BloatwareLog.AppId, job?.CorrelationId);
            WriteResult(jobPath, new BloatwareResult { CorrelationId = job?.CorrelationId ?? "", Action = job?.Action ?? "", Error = ex.ToString() });
        }
        Shutdown(0);
    }

    private static void WriteResult(string jobPath, BloatwareResult result)
    {
        if (jobPath.Length == 0)
            return;
        File.WriteAllText(Path.ChangeExtension(jobPath, ".result.json"), JsonSerializer.Serialize(result, Json));
    }
}
