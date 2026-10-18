using System.IO;
using System.Security.Principal;
using Vestigium.Logging;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ActiveSetup;

internal static class ActiveSetupLog
{
    public const string AppId = "AppMgmtIQ";
    public const string Category = "Suite";

    public const int Requested = 17065;
    public const int Completed = 17070;
    public const int Cancelled = 17075;
    public const int Changed = 17080;

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
            taxonomy.Register(Category, "ActiveSetup");
            cfg.RegisterTaxonomy(taxonomy);
            cfg.RegisterEvent("ActiveSetupRequested", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ActiveSetupRequested", Category, "ActiveSetup", Requested, "Information", "active setup change requested");
            cfg.RegisterEvent("ActiveSetupCompleted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ActiveSetupCompleted", Category, "ActiveSetup", Completed, "Information", "active setup change completed");
            cfg.RegisterEvent("ActiveSetupCancelled", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ActiveSetupCancelled", Category, "ActiveSetup", Cancelled, "Information", "active setup change cancelled");
            cfg.RegisterEvent("ActiveSetupChanged", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ActiveSetupChanged", Category, "ActiveSetup", Changed, "Information", "active setup key changed");
        });
        VestigiumLogger.BindLifetime(System.Windows.Application.Current);
    }

    public static void Information(int eventId, string message, string correlationId)
        => VestigiumLog.Write(
            eventId,
            VestigiumLogLevel.Information,
            VestigiumStatus.Success,
            Category,
            "ActiveSetup",
            message,
            null,
            AppId,
            correlationId,
            new Dictionary<string, string?> { ["user"] = WindowsIdentity.GetCurrent().Name });
}
