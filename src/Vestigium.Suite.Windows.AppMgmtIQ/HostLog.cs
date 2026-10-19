using System.IO;
using System.Security.Principal;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;
using Vestigium.Helpers.ClosedXml;
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
            ClosedXmlCatalog.Register(cfg);
            AnalyticsCatalog.Register(cfg);
            ChartsCatalog.Register(cfg);
            AppMgmtCatalog.Register(cfg);
            extra?.Invoke(cfg);
        });
        VestigiumLogger.BindLifetime(System.Windows.Application.Current);
    }

    private static IReadOnlyDictionary<string, string?> Actor =>
        new Dictionary<string, string?> { ["user"] = WindowsIdentity.GetCurrent().Name };

    public static void Information(int eventId, string subcategory, string message)
        => VestigiumLog.Write(eventId, VestigiumLogLevel.Information, VestigiumStatus.Success, AppMgmtCatalog.Category, subcategory, message, null, HostIds.AppMgmtIQ, null, Actor);

    public static void Information(int eventId, string subcategory, string message, string correlationId)
        => VestigiumLog.Write(eventId, VestigiumLogLevel.Information, VestigiumStatus.Success, AppMgmtCatalog.Category, subcategory, message, null, HostIds.AppMgmtIQ, correlationId, Actor);

    public static void Warning(int eventId, string subcategory, string message)
        => VestigiumLog.Write(eventId, VestigiumLogLevel.Warning, VestigiumStatus.Failed, AppMgmtCatalog.Category, subcategory, message, null, HostIds.AppMgmtIQ, null, Actor);
}
