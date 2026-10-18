using System.IO;
using System.Security.Principal;
using Vestigium.Logging;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Features;

internal static class FeatureLog
{
    public const string AppId = "AppMgmtIQ";
    public const string Category = "Suite";

    public const int Requested = 17045;
    public const int Completed = 17050;
    public const int Cancelled = 17055;
    public const int Changed = 17060;

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
            taxonomy.Register(Category, "Features");
            cfg.RegisterTaxonomy(taxonomy);
            cfg.RegisterEvent("FeatureApplyRequested", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FeatureApplyRequested", Category, "Features", Requested, "Information", "feature apply requested");
            cfg.RegisterEvent("FeatureApplyCompleted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FeatureApplyCompleted", Category, "Features", Completed, "Information", "feature apply completed");
            cfg.RegisterEvent("FeatureApplyCancelled", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FeatureApplyCancelled", Category, "Features", Cancelled, "Information", "feature apply cancelled");
            cfg.RegisterEvent("FeatureChanged", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FeatureChanged", Category, "Features", Changed, "Information", "feature enabled or disabled");
        });
        VestigiumLogger.BindLifetime(System.Windows.Application.Current);
    }

    public static void Information(int eventId, string message, string correlationId)
        => VestigiumLog.Write(
            eventId,
            VestigiumLogLevel.Information,
            VestigiumStatus.Success,
            Category,
            "Features",
            message,
            null,
            AppId,
            correlationId,
            new Dictionary<string, string?> { ["user"] = WindowsIdentity.GetCurrent().Name });
}
