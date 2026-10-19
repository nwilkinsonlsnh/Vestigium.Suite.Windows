using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class StartupViewModel : ObservableObject
{
    public ObservableCollection<StartupRow> Rows { get; } = [];

    [ObservableProperty]
    private StartupRow? _selected;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading startup entries\u2026";
}
