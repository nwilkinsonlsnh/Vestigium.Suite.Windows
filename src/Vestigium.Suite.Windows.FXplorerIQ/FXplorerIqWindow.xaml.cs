using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Vestigium.Controls.Shell;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class FXplorerIqWindow : Window
{
    private readonly List<ExplorerSlot> _explorers = [];
    private int _next = 1;
    private Action<string>? _status;

    public FXplorerIqWindow(VestigiumDefaultWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.RootShell = RootShell;
        SourceInitialized += OnSourceInitialized;
    }

    public VestigiumDefaultWindowViewModel ViewModel { get; }

    public VestigiumShell HostShell => RootShell;

    public IReadOnlyList<ExplorerSlot> Explorers => _explorers;

    public void Start(Action<string> status)
    {
        _status = status;
        if (_explorers.Count == 0)
            AddExplorer();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var work = SystemParameters.WorkArea;
        var width = Math.Min(1800, Math.Max(1200, work.Width - 48));
        var height = Math.Min(980, Math.Max(640, work.Height - 48));
        Width = width;
        Height = height;
        Left = work.Left + Math.Max(0, (work.Width - Width) / 2);
        Top = work.Top + Math.Max(0, (work.Height - Height) / 2);
    }

    private void AddExplorer_Click(object sender, RoutedEventArgs e) => AddExplorer();

    private void AddExplorer()
    {
        var number = _next++;
        var header = number == 1 ? "Explorer" : "Explorer " + number;
        var pane = new ExplorerPane { Status = text => _status?.Invoke(text) };
        var tab = new RadioButton
        {
            Content = header,
            Style = (Style)FindResource("RadioButton.UnderlineTab"),
            GroupName = "FXplorerMainNav",
            Margin = new Thickness(0, 0, 2, 0)
        };
        var close = new Button
        {
            Content = "×",
            Style = (Style)FindResource("Button.Utility"),
            Width = 24,
            Height = 24,
            MinWidth = 24,
            MinHeight = 24,
            Padding = new Thickness(0),
            FontSize = 14,
            ToolTip = "Close Explorer",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        var host = new StackPanel { Orientation = Orientation.Horizontal };
        var slot = new ExplorerSlot
        {
            Id = number.ToString(),
            Header = header,
            Pane = pane,
            Tab = tab,
            Close = close,
            Host = host
        };
        tab.Tag = slot;
        close.Tag = slot;
        tab.Checked += ExplorerTab_Checked;
        tab.MouseDoubleClick += ExplorerTab_DoubleClick;
        close.Click += CloseExplorer_Click;
        pane.Navigated += _ => RefreshLabel(slot);
        host.Children.Add(tab);
        host.Children.Add(close);
        _explorers.Add(slot);
        ExplorerTabs.Children.Add(host);
        RefreshClose();
        tab.IsChecked = true;
    }

    private void ExplorerTab_Checked(object sender, RoutedEventArgs e)
    {
        if (RootShell is null || sender is not RadioButton { Tag: ExplorerSlot slot })
            return;
        Show(slot);
    }

    private void ExplorerTab_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not RadioButton { Tag: ExplorerSlot slot })
            return;
        OpenRename(slot);
        e.Handled = true;
    }

    private void OpenRename(ExplorerSlot slot)
    {
        var box = new TextBox
        {
            Text = slot.Name ?? "",
            Style = (Style)FindResource("TextBox.Standard"),
            MinWidth = 180,
            MinHeight = 32,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var popup = new Popup
        {
            PlacementTarget = slot.Tab,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true
        };
        var panel = new StackPanel { Margin = new Thickness(8) };
        panel.Children.Add(new TextBlock
        {
            Text = "Name",
            Style = (Style)FindResource("TextBlock.Caption"),
            Margin = new Thickness(0, 0, 0, 6)
        });
        panel.Children.Add(box);
        popup.Child = new Border
        {
            Background = (System.Windows.Media.Brush)FindResource("Vestigium.Brushes.Surface.Window"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("Vestigium.Brushes.Stroke.Default"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = panel
        };
        box.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                CommitName(slot, box.Text);
                popup.IsOpen = false;
                args.Handled = true;
            }
            else if (args.Key == Key.Escape)
            {
                popup.IsOpen = false;
                args.Handled = true;
            }
        };
        popup.IsOpen = true;
        box.Focus();
        box.SelectAll();
    }

    private void CommitName(ExplorerSlot slot, string text)
    {
        var name = text.Trim();
        slot.Name = name.Length == 0 ? null : name;
        RefreshLabel(slot);
    }

    private void RefreshLabel(ExplorerSlot slot)
    {
        slot.Tab.Content = string.IsNullOrWhiteSpace(slot.Name)
            ? FolderLabel(slot.Pane.Folder)
            : slot.Name;
    }

    private static string FolderLabel(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Explorer";
        var name = Path.GetFileName(path.TrimEnd('\\', '/'));
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }

    private void CloseExplorer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ExplorerSlot slot } || _explorers.Count <= 1)
            return;

        var index = _explorers.IndexOf(slot);
        var wasSelected = slot.Tab.IsChecked == true;
        _explorers.RemoveAt(index);
        ExplorerTabs.Children.Remove(slot.Host);
        RefreshClose();

        if (!wasSelected)
            return;

        var next = _explorers[Math.Min(index, _explorers.Count - 1)];
        next.Tab.IsChecked = true;
    }

    private void Show(ExplorerSlot slot)
    {
        var item = RootShell["Explorer"];
        if (item is null)
            return;
        item.Content = slot.Pane;
        RootShell.SelectedItem = item;
    }

    private void RefreshClose()
    {
        var show = _explorers.Count > 1;
        foreach (var slot in _explorers)
            slot.Close.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MainNav_Checked(object sender, RoutedEventArgs e)
    {
        if (RootShell is null || sender is not RadioButton { Content: string header })
            return;

        var item = RootShell[header];
        if (item is not null)
            RootShell.SelectedItem = item;
    }
}
