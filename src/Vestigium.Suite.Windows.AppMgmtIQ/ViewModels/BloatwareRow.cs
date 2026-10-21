namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class BloatwareRow
{
    public string BaselineStatus { get; set; } = "";
    public required string Name { get; init; }
    public required string Family { get; init; }
    public required string Category { get; init; }
    public required bool Installed { get; init; }
    public required bool Provisioned { get; init; }
    public required string Package { get; init; }

    public string InstalledText => Installed ? "Yes" : "No";
    public string ProvisionedText => Provisioned ? "Yes" : "No";
}
