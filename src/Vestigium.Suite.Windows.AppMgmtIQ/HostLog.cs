using System.IO;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.WinReg;
using Vestigium.Logging;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public static class HostLog
{
    public static void Initialize(string appId, Action<VestigiumLoggerOptions>? extra = null)
    {
        if (string.IsNullOrWhiteSpace(appId))
            throw new ArgumentException("Host APPID is required.", nameof(appId));

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Vestigium",
            "Logs",
            appId);

        Directory.CreateDirectory(root);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = appId;
            cfg.LogDirectory = root;
            WinRegCatalog.Register(cfg);
            KqlLoggingCatalog.Register(cfg);
            AppMgmtCatalog.Register(cfg);
            extra?.Invoke(cfg);
        });
    }

    public static void Information(int eventId, string subcategory, string message)
        => VestigiumLog.Information(eventId, VestigiumStatus.Success, AppMgmtCatalog.Category, subcategory, message, appId: HostIds.AppMgmtIQ);
}
