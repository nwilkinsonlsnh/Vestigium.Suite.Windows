using Vestigium.Logging;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public static class AppMgmtCatalog
{
    public const string Category = "Suite";
    public const string Subcategory = "AppMgmtIQ";

    public const int AppStarted = 17000;
    public const int InventoryLoaded = 17005;
    public const int SettingsChanged = 17010;
    public const int SettingsSaved = 17015;
    public const int FeaturesLoaded = 17020;
    public const int ExportWritten = 17025;
    public const int BloatwareLoaded = 17030;
    public const int NoRemoveCleared = 17035;
    public const int UninstallStarted = 17040;

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, HostIds.AppMgmtIQ);
        taxonomy.Register(Category, "Lifecycle", "Inventory", "Settings", "Features", "Export", "Bloatware", "Uninstall");
        cfg.RegisterTaxonomy(taxonomy);
        cfg.RegisterEvent("AppStarted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.AppStarted", Category, Subcategory, AppStarted, "Information", "host started");
        cfg.RegisterEvent("InventoryLoaded", "Vestigium.Suite.Windows.AppMgmtIQ.Events.InventoryLoaded", Category, Subcategory, InventoryLoaded, "Information", "inventory loaded");
        cfg.RegisterEvent("SettingsChanged", "Vestigium.Suite.Windows.AppMgmtIQ.Events.SettingsChanged", Category, Subcategory, SettingsChanged, "Information", "setting changed");
        cfg.RegisterEvent("SettingsSaved", "Vestigium.Suite.Windows.AppMgmtIQ.Events.SettingsSaved", Category, Subcategory, SettingsSaved, "Information", "settings saved");
        cfg.RegisterEvent("FeaturesLoaded", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FeaturesLoaded", Category, Subcategory, FeaturesLoaded, "Information", "windows features loaded");
        cfg.RegisterEvent("ExportWritten", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ExportWritten", Category, "Export", ExportWritten, "Information", "workbook written");
        cfg.RegisterEvent("BloatwareLoaded", "Vestigium.Suite.Windows.AppMgmtIQ.Events.BloatwareLoaded", Category, "Bloatware", BloatwareLoaded, "Information", "bloatware catalog matched");
        cfg.RegisterEvent("NoRemoveCleared", "Vestigium.Suite.Windows.AppMgmtIQ.Events.NoRemoveCleared", Category, "Uninstall", NoRemoveCleared, "Warning", "NoRemove cleared by an administrator");
        cfg.RegisterEvent("UninstallStarted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.UninstallStarted", Category, "Uninstall", UninstallStarted, "Information", "uninstall started");
    }
}
