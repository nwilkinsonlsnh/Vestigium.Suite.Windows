using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Helpers.WinReg;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class DetailsWindow : Window
{
    private PackageRow? _package;

    public DetailsWindow()
    {
        InitializeComponent();
    }

    public void SetPackage(PackageRow package)
    {
        _package = package;
        var previous = (DataContext as DetailsViewModel)?.SelectedTab?.Header ?? "General";
        var vm = new DetailsViewModel(package);
        vm.SelectedTab = vm.Tabs.FirstOrDefault(t => t.Header == previous) ?? vm.Tabs[0];
        DataContext = vm;
        UpdateLayout();
        CheckRadio(vm.SelectedTab?.Header);
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DetailsViewModel vm || sender is not RadioButton { DataContext: DetailsTab tab })
            return;
        vm.SelectedTab = tab;
    }

    private void Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
        if (cell is null)
            return;
        var info = new DataGridCellInfo(cell);
        if (!ValueGrid.SelectedCells.Contains(info))
        {
            ValueGrid.SelectedCells.Clear();
            ValueGrid.SelectedCells.Add(info);
        }
        cell.Focus();
    }

    private void Menu_Opened(object sender, RoutedEventArgs e)
    {
        var cells = ValueGrid.SelectedCells;
        CopyItem.IsEnabled = cells.Count > 0;
        var columns = cells.Select(cell => cell.Column.Header?.ToString()).Distinct().Count();
        CopyDataItem.Visibility = columns <= 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_package is null)
            return;
        if (!Enum.TryParse<RegistryHiveKind>(_package.Hive, out var hive))
            return;
        var key = RegistryHelper.Local.GetKey(hive, _package.KeyPath, RegistryViewKind.Default, RegistryDetailLevel.Full);
        if (key is null)
            return;
        SetPackage(new PackageRow
        {
            DisplayName = _package.DisplayName,
            Publisher = _package.Publisher,
            DisplayVersion = _package.DisplayVersion,
            InstallDate = _package.InstallDate,
            EstimatedSize = _package.EstimatedSize,
            IsX64 = _package.IsX64,
            IsGlobal = _package.IsGlobal,
            SystemComponent = _package.SystemComponent,
            WindowsInstaller = _package.WindowsInstaller,
            NoModify = _package.NoModify,
            NoRemove = _package.NoRemove,
            NoRepair = _package.NoRepair,
            UninstallString = _package.UninstallString,
            KeyPath = _package.KeyPath,
            Hive = _package.Hive,
            Values = key.Values,
            Icon = _package.Icon
        });
    }

    private void CopyData_Click(object sender, RoutedEventArgs e)
    {
        var lines = ValueGrid.SelectedCells
            .Select(cell => cell.Item)
            .OfType<ValueRow>()
            .Distinct()
            .Select(row => row.Data);
        var text = string.Join(Environment.NewLine, lines);
        if (text.Length > 0)
            Clipboard.SetText(text);
    }

    private void CopyInfo_Click(object sender, RoutedEventArgs e)
    {
        var cells = ValueGrid.SelectedCells.ToList();
        if (cells.Count == 0)
            return;
        var headers = cells
            .Select(cell => cell.Column.Header?.ToString() ?? "")
            .Distinct()
            .ToList();
        var rows = cells
            .Select(cell => cell.Item)
            .OfType<ValueRow>()
            .Distinct()
            .ToList();
        var lines = new List<string> { string.Join("\t", headers) };
        foreach (var row in rows)
        {
            var values = headers.Select(header => header switch
            {
                "Name" => row.Name,
                "Type" => row.Type,
                "Data" => row.Data,
                _ => ""
            });
            lines.Add(string.Join("\t", values));
        }

        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }

    private void CheckRadio(string? header)
    {
        var match = FindRadio(TabStrip, header) ?? FindRadio(TabStrip, null);
        if (match is not null)
            match.IsChecked = true;
    }

    private static RadioButton? FindRadio(DependencyObject start, string? header)
    {
        var count = VisualTreeHelper.GetChildrenCount(start);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(start, i);
            if (child is RadioButton radio && (header is null || Equals(radio.Content, header) || Equals(radio.DataContext is DetailsTab tab ? tab.Header : null, header)))
                return radio;
            var nested = FindRadio(child, header);
            if (nested is not null)
                return nested;
        }
        return null;
    }

    private static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
    {
        while (start is not null)
        {
            if (start is T match)
                return match;
            start = VisualTreeHelper.GetParent(start);
        }
        return null;
    }
}
