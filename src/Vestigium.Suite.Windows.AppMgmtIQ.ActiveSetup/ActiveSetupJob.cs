namespace Vestigium.Suite.Windows.AppMgmtIQ.ActiveSetup;

public sealed class ActiveSetupJob
{
    public string CorrelationId { get; set; } = "";
    public string ThemeId { get; set; } = "";
    public double ParentLeft { get; set; }
    public double ParentTop { get; set; }
    public double ParentWidth { get; set; }
    public double ParentHeight { get; set; }
    public string Action { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Component { get; set; } = "";
    public string StubPath { get; set; } = "";
    public string Version { get; set; } = "1,0";
    public bool IsX64 { get; set; } = true;
    public bool Installed { get; set; } = true;
}

public sealed class ActiveSetupResult
{
    public string CorrelationId { get; set; } = "";
    public bool Cancelled { get; set; }
    public bool Ok { get; set; }
    public string Action { get; set; } = "";
    public string KeyPath { get; set; } = "";
    public string Error { get; set; } = "";
}
