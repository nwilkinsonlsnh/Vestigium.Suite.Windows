namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class StartupRow
{
    public string BaselineStatus { get; set; } = "";
    public string Name { get; init; } = "";
    public string Key { get; init; } = "";
    public string Command { get; init; } = "";
    public string Source { get; init; } = "";
    public string Scope { get; init; } = "";
    public string Bit { get; init; } = "";
    public string Package { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string State => Enabled ? "Enabled" : "Disabled";
}
