using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class FootprintsViewModel : ObservableObject
{
    private readonly List<FootprintRow> _all = [];

    public ObservableCollection<FootprintRow> Rows { get; } = [];
    public IReadOnlyList<FootprintRow> All => _all;

    [ObservableProperty]
    private FootprintRow? _selected;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading footprints\u2026";

    public Task LoadAsync()
    {
        IsLoading = false;
        Status = "0 entries";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}
