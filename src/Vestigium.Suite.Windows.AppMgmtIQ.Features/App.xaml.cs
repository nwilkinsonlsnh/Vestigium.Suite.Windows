using System.IO;
using System.Text.Json;
using System.Windows;
using Vestigium.Themes;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Features;

public partial class App : Application
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private void OnStartup(object sender, StartupEventArgs e)
    {
        FeatureLog.Initialize();
        if (e.Args.Length == 0 || !File.Exists(e.Args[0]))
        {
            Shutdown(1);
            return;
        }

        var job = JsonSerializer.Deserialize<FeatureJob>(File.ReadAllText(e.Args[0]), Json) ?? new FeatureJob();
        var themes = new ThemeManager();
        themes.RegisterSuiteV1();
        themes.Initialize(this, string.IsNullOrWhiteSpace(job.ThemeId) ? "LightBlue" : job.ThemeId);
        var window = new ApplyFeatureChangesWindow(job);
        if (job.ParentWidth > 0 && job.ParentHeight > 0)
        {
            window.Left = job.ParentLeft + (job.ParentWidth - window.Width) / 2;
            window.Top = job.ParentTop + (job.ParentHeight - window.Height) / 2;
        }
        window.ShowDialog();
        var resultPath = Path.ChangeExtension(e.Args[0], ".result.json");
        File.WriteAllText(resultPath, JsonSerializer.Serialize(window.Result, Json));
        Shutdown(0);
    }
}
