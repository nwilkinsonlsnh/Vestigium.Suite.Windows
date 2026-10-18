using System.IO;
using System.Security.Principal;
using Vestigium.Logging;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Bloatware;

internal static class BloatwareLog
{
    public const string AppId = "AppMgmtIQ";
    public const string Category = "Suite";
    public const int Requested = 17090;
    public const int Completed = 17095;
    public const int Changed = 17100;

    public static void Initialize()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vestigium", "Logs", AppId);
        Directory.CreateDirectory(root);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = AppId;
            cfg.LogDirectory = root;
            var taxonomy = new VestigiumTaxonomy();
            taxonomy.Register(Category, AppId);
            taxonomy.Register(Category, "Bloatware");
            cfg.RegisterTaxonomy(taxonomy);
            cfg.RegisterEvent("BloatwareRequested", "Vestigium.Suite.Windows.AppMgmtIQ.Events.BloatwareRequested", Category, "Bloatware", Requested, "Information", "bloatware change requested");
            cfg.RegisterEvent("BloatwareCompleted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.BloatwareCompleted", Category, "Bloatware", Completed, "Information", "bloatware change completed");
            cfg.RegisterEvent("BloatwareChanged", "Vestigium.Suite.Windows.AppMgmtIQ.Events.BloatwareChanged", Category, "Bloatware", Changed, "Information", "bloatware package changed");
        });
        VestigiumLogger.BindLifetime(System.Windows.Application.Current);
    }

    public static void Information(int eventId, string message, string correlationId)
        => VestigiumLog.Write(eventId, VestigiumLogLevel.Information, VestigiumStatus.Success, Category, "Bloatware", message, null, AppId, correlationId, new Dictionary<string, string?> { ["user"] = WindowsIdentity.GetCurrent().Name });
}
