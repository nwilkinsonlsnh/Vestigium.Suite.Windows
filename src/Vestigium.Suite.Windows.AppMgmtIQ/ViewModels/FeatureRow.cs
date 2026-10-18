namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

/// <summary>
/// One optional Windows feature. Debt: the WMI walk belongs in a Helper door.
/// </summary>
public sealed class FeatureRow
{
    public required string Name { get; init; }
    public required string Caption { get; init; }
    public required int InstallState { get; init; }
    public string Description { get; set; } = "";
    public required string Status { get; init; }

    public string State => InstallState switch
    {
        1 => "Enabled",
        2 => "Disabled",
        3 => "Absent",
        _ => "Unknown"
    };

    public bool IsEnabled => InstallState == 1;
}
