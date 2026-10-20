namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class FootprintRow
{
    public string Kind { get; init; } = "";
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string Scope { get; init; } = "";
    public int? RefCount { get; init; }
    public string Package { get; set; } = "";
    public string RefCountText => RefCount?.ToString() ?? "";
}
