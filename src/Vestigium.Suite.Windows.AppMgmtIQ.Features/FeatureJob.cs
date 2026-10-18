namespace Vestigium.Suite.Windows.AppMgmtIQ.Features;

public sealed class FeatureJob
{
    public string CorrelationId { get; set; } = "";
    public List<FeatureJobItem> Enable { get; set; } = [];
    public List<FeatureJobItem> Disable { get; set; } = [];
}

public sealed class FeatureJobItem
{
    public string Caption { get; set; } = "";
    public string Name { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool IsAbsent { get; set; }
}

public sealed class FeatureResult
{
    public string CorrelationId { get; set; } = "";
    public bool Cancelled { get; set; }
    public List<string> Enabled { get; set; } = [];
    public List<string> Disabled { get; set; } = [];
    public List<string> Restart { get; set; } = [];
    public List<string> Failed { get; set; } = [];
}
