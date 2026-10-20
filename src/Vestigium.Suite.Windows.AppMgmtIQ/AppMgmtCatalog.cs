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
    public const int ActiveSetupLoaded = 17085;
    public const int NoRemoveCleared = 17035;
    public const int UninstallStarted = 17040;
    public const int FeatureApplyRequested = 17045;
    public const int FeatureApplyCompleted = 17050;
    public const int FeatureApplyCancelled = 17055;
    public const int FeatureChanged = 17060;
    public const int ActiveSetupRequested = 17065;
    public const int ActiveSetupCompleted = 17070;
    public const int ActiveSetupCancelled = 17075;
    public const int ActiveSetupChanged = 17080;
    public const int BloatwareRequested = 17090;
    public const int BloatwareCompleted = 17095;
    public const int BloatwareChanged = 17100;
    public const int StartupLoaded = 17105;
    public const int StartupChanged = 17110;
    public const int StartupRequested = 17115;
    public const int StartupCompleted = 17120;
    public const int StartupCancelled = 17125;
    public const int FootprintsLoaded = 17130;
    public const int LifecycleRequested = 17135;
    public const int LifecycleCompleted = 17140;

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, HostIds.AppMgmtIQ);
        taxonomy.Register(Category, "Lifecycle", "Inventory", "Settings", "Features", "Export", "Bloatware", "Uninstall", "ActiveSetup", "Startup", "Footprints");
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
        cfg.RegisterEvent("FeatureApplyRequested", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FeatureApplyRequested", Category, "Features", FeatureApplyRequested, "Information", "feature apply requested");
        cfg.RegisterEvent("FeatureApplyCompleted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FeatureApplyCompleted", Category, "Features", FeatureApplyCompleted, "Information", "feature apply completed");
        cfg.RegisterEvent("FeatureApplyCancelled", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FeatureApplyCancelled", Category, "Features", FeatureApplyCancelled, "Information", "feature apply cancelled");
        cfg.RegisterEvent("FeatureChanged", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FeatureChanged", Category, "Features", FeatureChanged, "Information", "feature enabled or disabled");
        cfg.RegisterEvent("ActiveSetupLoaded", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ActiveSetupLoaded", Category, "ActiveSetup", ActiveSetupLoaded, "Information", "active setup loaded");
        cfg.RegisterEvent("ActiveSetupRequested", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ActiveSetupRequested", Category, "ActiveSetup", ActiveSetupRequested, "Information", "active setup change requested");
        cfg.RegisterEvent("ActiveSetupCompleted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ActiveSetupCompleted", Category, "ActiveSetup", ActiveSetupCompleted, "Information", "active setup change completed");
        cfg.RegisterEvent("ActiveSetupCancelled", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ActiveSetupCancelled", Category, "ActiveSetup", ActiveSetupCancelled, "Information", "active setup change cancelled");
        cfg.RegisterEvent("ActiveSetupChanged", "Vestigium.Suite.Windows.AppMgmtIQ.Events.ActiveSetupChanged", Category, "ActiveSetup", ActiveSetupChanged, "Information", "active setup key changed");
        cfg.RegisterEvent("BloatwareRequested", "Vestigium.Suite.Windows.AppMgmtIQ.Events.BloatwareRequested", Category, "Bloatware", BloatwareRequested, "Information", "bloatware change requested");
        cfg.RegisterEvent("BloatwareCompleted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.BloatwareCompleted", Category, "Bloatware", BloatwareCompleted, "Information", "bloatware change completed");
        cfg.RegisterEvent("BloatwareChanged", "Vestigium.Suite.Windows.AppMgmtIQ.Events.BloatwareChanged", Category, "Bloatware", BloatwareChanged, "Information", "bloatware package changed");
        cfg.RegisterEvent("StartupLoaded", "Vestigium.Suite.Windows.AppMgmtIQ.Events.StartupLoaded", Category, "Startup", StartupLoaded, "Information", "startup entries loaded");
        cfg.RegisterEvent("StartupChanged", "Vestigium.Suite.Windows.AppMgmtIQ.Events.StartupChanged", Category, "Startup", StartupChanged, "Information", "startup entry enabled or disabled");
        cfg.RegisterEvent("StartupRequested", "Vestigium.Suite.Windows.AppMgmtIQ.Events.StartupRequested", Category, "Startup", StartupRequested, "Information", "startup change requested");
        cfg.RegisterEvent("StartupCompleted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.StartupCompleted", Category, "Startup", StartupCompleted, "Information", "startup change completed");
        cfg.RegisterEvent("StartupCancelled", "Vestigium.Suite.Windows.AppMgmtIQ.Events.StartupCancelled", Category, "Startup", StartupCancelled, "Information", "startup change cancelled");
        cfg.RegisterEvent("FootprintsLoaded", "Vestigium.Suite.Windows.AppMgmtIQ.Events.FootprintsLoaded", Category, "Footprints", FootprintsLoaded, "Information", "footprints loaded");
        cfg.RegisterEvent("LifecycleRequested", "Vestigium.Suite.Windows.AppMgmtIQ.Events.LifecycleRequested", Category, "Lifecycle", LifecycleRequested, "Information", "modify or repair started");
        cfg.RegisterEvent("LifecycleCompleted", "Vestigium.Suite.Windows.AppMgmtIQ.Events.LifecycleCompleted", Category, "Lifecycle", LifecycleCompleted, "Information", "modify or repair finished");
    }
}
