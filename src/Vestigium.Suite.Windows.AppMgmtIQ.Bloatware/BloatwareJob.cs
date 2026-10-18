namespace Vestigium.Suite.Windows.AppMgmtIQ.Bloatware;

public sealed class BloatwareJob
{
    public string CorrelationId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Name { get; set; } = "";
    public string Family { get; set; } = "";
    public string Package { get; set; } = "";
    public bool Provisioned { get; set; }
}

public sealed class BloatwareResult
{
    public string CorrelationId { get; set; } = "";
    public string Action { get; set; } = "";
    public bool Ok { get; set; }
    public bool OpenStore { get; set; }
    public string Detail { get; set; } = "";
    public string Error { get; set; } = "";
}
