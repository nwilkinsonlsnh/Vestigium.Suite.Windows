using Vestigium.Logging;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public static class FXplorerCatalog
{
    public const string Category = "Suite";
    public const string Subcategory = "FXplorerIQ";

    public const int AppStarted = 18000;

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, HostIds.FXplorerIQ);
        taxonomy.Register(Category, "Lifecycle");
        cfg.RegisterTaxonomy(taxonomy);
        cfg.RegisterEvent("AppStarted", "Vestigium.Suite.Windows.FXplorerIQ.Events.AppStarted", Category, Subcategory, AppStarted, "Information", "host started");
    }
}
