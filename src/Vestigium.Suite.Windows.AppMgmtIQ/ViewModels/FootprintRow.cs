using CommunityToolkit.Mvvm.ComponentModel;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class FootprintRow : ObservableObject
{
    public string Kind { get; init; } = "";
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string Scope { get; init; } = "";
    public int? RefCount { get; init; }
    public string Package { get; set; } = "";
    public string RefCountText => RefCount?.ToString() ?? "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExistsText))]
    private bool? _exists;

    public string ExistsText => Exists switch { true => "Yes", false => "No", _ => "" };
}
