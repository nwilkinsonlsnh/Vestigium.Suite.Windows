using System.IO;
using System.Security.Principal;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Hashing;
using Vestigium.Logging;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public static class HostLog
{
    public static void Initialize(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
            throw new ArgumentException("Host APPID is required.", nameof(appId));

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Vestigium",
            appId,
            "Logs");

        Directory.CreateDirectory(root);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = appId;
            cfg.LogDirectory = root;
            FileIoCatalog.Register(cfg);
            AnalyticsCatalog.Register(cfg);
            ChartsCatalog.Register(cfg);
            HashingCatalog.Register(cfg);
            FXplorerCatalog.Register(cfg);
        });
        VestigiumLogger.BindLifetime(System.Windows.Application.Current);
    }

    private static IReadOnlyDictionary<string, string?> Actor =>
        new Dictionary<string, string?> { ["user"] = WindowsIdentity.GetCurrent().Name };

    public static void Information(int eventId, string subcategory, string message)
        => VestigiumLog.Write(eventId, VestigiumLogLevel.Information, VestigiumStatus.Success, FXplorerCatalog.Category, subcategory, message, null, HostIds.FXplorerIQ, null, Actor);
}
