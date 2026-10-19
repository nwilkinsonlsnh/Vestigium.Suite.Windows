namespace Vestigium.Suite.Windows.AppMgmtIQ.Startup;

public sealed class StartupJob
{
    public string CorrelationId { get; set; } = "";
    public List<StartupJobItem> Items { get; set; } = [];
}

public sealed class StartupJobItem
{
    public string Name { get; set; } = "";
    public string Key { get; set; } = "";
    public string Branch { get; set; } = "Run";
    public string Action { get; set; } = "Disable";
    public bool IsX64 { get; set; } = true;
}

public sealed class StartupResult
{
    public string CorrelationId { get; set; } = "";
    public List<string> Changed { get; set; } = [];
    public List<string> Denied { get; set; } = [];
    public List<string> Failed { get; set; } = [];
}
