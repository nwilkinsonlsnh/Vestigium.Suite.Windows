using System.IO;
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
            extra?.Invoke(cfg);
        });
    }
}
