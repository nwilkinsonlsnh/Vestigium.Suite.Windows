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
        var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row is null)
            return;
        row.IsSelected = true;
        row.Focus();
    }

    private void Menu_Opened(object sender, RoutedEventArgs e)
    {
        CopyItem.IsEnabled = (DataContext as DetailsViewModel)?.SelectedRow is not null;
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

    private void CopyInfo_Click(object sender, RoutedEventArgs e) => Copy(row => $"{row.Name}\t{row.Type}\t{row.Data}");

    private void CopyName_Click(object sender, RoutedEventArgs e) => Copy(row => row.Name);

    private void CopyType_Click(object sender, RoutedEventArgs e) => Copy(row => row.Type);

    private void CopyData_Click(object sender, RoutedEventArgs e) => Copy(row => row.Data);

    private void Copy(Func<ValueRow, string> pick)
    {
        if ((DataContext as DetailsViewModel)?.SelectedRow is not ValueRow row)
            return;
        Clipboard.SetText(pick(row));
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
