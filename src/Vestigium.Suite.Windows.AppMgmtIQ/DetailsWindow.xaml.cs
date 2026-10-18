using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Helpers.WinReg;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class DetailsWindow : Window
{
    private PackageRow? _package;
    private int _anchorColumn;

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

    private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var header = FindAncestor<DataGridColumnHeader>(e.OriginalSource as DependencyObject);
        if (header?.Column is null)
            return;
        var index = header.Column.DisplayIndex;
        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        if (shift)
            SelectColumns(_anchorColumn, index);
        else if (ctrl)
            ToggleColumn(index);
        else
        {
            _anchorColumn = index;
            SelectColumns(index, index);
        }
        e.Handled = true;
    }

    private void SelectColumns(int from, int to)
    {
        var start = Math.Min(from, to);
        var end = Math.Max(from, to);
        ValueGrid.SelectedCells.Clear();
        foreach (var item in ValueGrid.Items)
        {
            for (var i = start; i <= end; i++)
            {
                var column = ValueGrid.Columns.FirstOrDefault(c => c.DisplayIndex == i);
                if (column is not null)
                    ValueGrid.SelectedCells.Add(new DataGridCellInfo(item, column));
            }
        }
    }

    private void ToggleColumn(int index)
    {
        var column = ValueGrid.Columns.FirstOrDefault(c => c.DisplayIndex == index);
        if (column is null)
            return;
        var selected = ValueGrid.SelectedCells.Any(cell => cell.Column == column);
        if (selected)
        {
            foreach (var cell in ValueGrid.SelectedCells.Where(cell => cell.Column == column).ToList())
                ValueGrid.SelectedCells.Remove(cell);
            return;
        }

        foreach (var item in ValueGrid.Items)
            ValueGrid.SelectedCells.Add(new DataGridCellInfo(item, column));
        _anchorColumn = index;
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
