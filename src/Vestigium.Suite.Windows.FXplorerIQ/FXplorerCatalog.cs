using Vestigium.Logging;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public static class FXplorerCatalog
{
    public const string Category = "Suite";
    public const string Subcategory = "FXplorerIQ";

    public const int AppStarted = 18000;
    public const int ExplorerOpened = 18001;
    public const int ExplorerClosed = 18002;
    public const int SplitOpened = 18003;
    public const int SplitClosed = 18004;

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, HostIds.FXplorerIQ);
        taxonomy.Register(Category, "Lifecycle");
        cfg.RegisterTaxonomy(taxonomy);
        cfg.RegisterEvent("AppStarted", "Vestigium.Suite.Windows.FXplorerIQ.Events.AppStarted", Category, Subcategory, AppStarted, "Information", "host started");
        cfg.RegisterEvent("ExplorerOpened", "Vestigium.Suite.Windows.FXplorerIQ.Events.ExplorerOpened", Category, Subcategory, ExplorerOpened, "Information", "explorer opened");
        cfg.RegisterEvent("ExplorerClosed", "Vestigium.Suite.Windows.FXplorerIQ.Events.ExplorerClosed", Category, Subcategory, ExplorerClosed, "Information", "explorer closed");
        cfg.RegisterEvent("SplitOpened", "Vestigium.Suite.Windows.FXplorerIQ.Events.SplitOpened", Category, Subcategory, SplitOpened, "Information", "split opened");
        cfg.RegisterEvent("SplitClosed", "Vestigium.Suite.Windows.FXplorerIQ.Events.SplitClosed", Category, Subcategory, SplitClosed, "Information", "split closed");
    }
}
