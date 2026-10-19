namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class StartupLaunchJob
{
    public string CorrelationId { get; set; } = "";
    public List<StartupLaunchItem> Items { get; set; } = [];
}

public sealed class StartupLaunchItem
{
    public string Name { get; set; } = "";
    public string Key { get; set; } = "";
    public string Source { get; set; } = "Run";
    public string Branch { get; set; } = "Run";
    public string Action { get; set; } = "Disable";
    public bool IsX64 { get; set; } = true;
}

public sealed class StartupLaunchResult
{
    public string CorrelationId { get; set; } = "";
    public List<string> Changed { get; set; } = [];
    public List<string> Denied { get; set; } = [];
    public List<string> Failed { get; set; } = [];
}
