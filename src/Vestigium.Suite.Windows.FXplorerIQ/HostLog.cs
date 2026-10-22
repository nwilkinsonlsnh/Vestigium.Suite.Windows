using System.IO;
using Vestigium.Helpers.FileIo;
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
        });
        VestigiumLogger.BindLifetime(System.Windows.Application.Current);
    }
}
