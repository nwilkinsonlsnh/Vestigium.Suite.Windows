using System.IO;
using System.Security.Principal;
using Vestigium.Logging;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Startup;

internal static class StartupLog
{
    public const string AppId = "AppMgmtIQ";
    public const string Category = "Suite";

    public const int Requested = 17115;
    public const int Completed = 17120;
    public const int Changed = 17110;

    public static void Initialize()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Vestigium",
            "Logs",
            AppId);
        Directory.CreateDirectory(root);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = AppId;
            cfg.LogDirectory = root;
            var taxonomy = new VestigiumTaxonomy();
            taxonomy.Register(Category, AppId);
            taxonomy.Register(Category, "Startup");
            cfg.RegisterTaxonomy(taxonomy);
            cfg.RegisterEvent("StartupRequested", "Vestigium.Suite.Windows.AppMgmtIQ.Events.StartupRequested", Category, "Startup", Requested, "Information", "startup change requested");
            cfg.RegisterEvent("StartupCompleted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.StartupCompleted", Category, "Startup", Completed, "Information", "startup change completed");
            cfg.RegisterEvent("StartupChanged", "Vestigium.Suite.Windows.AppMgmtIQ.Events.StartupChanged", Category, "Startup", Changed, "Information", "startup entry enabled or disabled");
        });
        VestigiumLogger.BindLifetime(System.Windows.Application.Current);
    }

    public static void Information(int eventId, string message, string correlationId)
        => VestigiumLog.Write(
            eventId,
            VestigiumLogLevel.Information,
            VestigiumStatus.Success,
            Category,
            "Startup",
            message,
            null,
            AppId,
            correlationId,
            new Dictionary<string, string?> { ["user"] = WindowsIdentity.GetCurrent().Name });
}
