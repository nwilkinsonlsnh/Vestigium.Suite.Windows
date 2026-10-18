namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class ActiveSetupRow
{
    public required string DisplayName { get; init; }
    public required string Component { get; init; }
    public string StubPath { get; init; } = "";
    public string Version { get; init; } = "";
    public string Locale { get; init; } = "";
    public string ComponentId { get; init; } = "";
    public bool Installed { get; init; }
    public bool IsGlobal { get; init; }
    public bool IsX64 { get; init; }
    public bool Pending { get; init; }

    public string Scope => IsGlobal ? "Machine" : "User";
    public string Bit => IsX64 ? "x64" : "x86";
    public string InstalledText => Installed ? "Yes" : "No";
    public string PendingText => Pending ? "Yes" : "No";
}
