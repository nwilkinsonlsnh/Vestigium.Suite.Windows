using System.IO;
using Vestigium.Logging;
using Vestigium.Suite.Windows.AppMgmtIQ;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Features;

internal static class FeatureLog
{
    public static void Initialize()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Vestigium",
            "Logs",
            HostIds.AppMgmtIQ);
        Directory.CreateDirectory(root);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = HostIds.AppMgmtIQ;
            cfg.LogDirectory = root;
            AppMgmtCatalog.Register(cfg);
        });
    }

    public static void Information(int eventId, string message, string correlationId)
        => VestigiumLog.Write(
            eventId,
            VestigiumLogLevel.Information,
            VestigiumStatus.Success,
            AppMgmtCatalog.Category,
            "Features",
            message,
            null,
            HostIds.AppMgmtIQ,
            correlationId,
            null);
}
