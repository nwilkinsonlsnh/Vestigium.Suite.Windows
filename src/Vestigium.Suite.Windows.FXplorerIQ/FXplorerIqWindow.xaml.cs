using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Vestigium.Controls.Shell;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public partial class FXplorerIqWindow : Window
{
    private readonly List<ExplorerSlot> _explorers = [];
    private readonly List<ExplorerSlot> _hidden = [];
    private readonly ExplorerSessionStore _session = new(FXplorerSettingsStore.DefaultRoot);
    private int _next = 1;
    private Action<string>? _status;
    private bool _fitting;

    public FXplorerIqWindow(VestigiumDefaultWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.RootShell = RootShell;
        SourceInitialized += OnSourceInitialized;
        Closing += (_, _) => SaveSession();
    }

    public VestigiumDefaultWindowViewModel ViewModel { get; }

    public VestigiumShell HostShell => RootShell;

    public IReadOnlyList<ExplorerSlot> Explorers => _explorers;

    public void Start(Action<string> status)
    {
        _status = status;
        if (_explorers.Count > 0)
            return;

        var records = _session.Load().Explorers;
        if (records.Count == 0)
        {
            AddExplorer();
            return;
        }

        for (var i = 0; i < records.Count; i++)
            AddExplorer(records[i].Path, records[i].Name, select: i == 0, split: records[i].Split, second: records[i].SecondPath, sourceIsFirst: records[i].SourceIsFirst);
    }

    private void SaveSession()
    {
        var session = new ExplorerSession();
        foreach (var slot in _explorers)
            session.Explorers.Add(new ExplorerRecord
            {
                Name = slot.Name,
                Path = slot.Pane.Folder,
                Split = slot.View.IsSplit,
                SecondPath = slot.View.Second.Folder,
                SourceIsFirst = slot.View.SourceIsFirst
            });
        _session.Save(session);
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

    private void AddExplorer(string? path = null, string? name = null, bool select = true, bool split = false, string? second = null, bool sourceIsFirst = true)
    {
        var number = _next++;
        var header = number == 1 ? "Explorer" : "Explorer " + number;
        var view = new ExplorerHost(text => _status?.Invoke(text));
        if (!string.IsNullOrWhiteSpace(path))
            view.First.Seed(path);
        view.Apply(split, App.Settings.Load(), second, sourceIsFirst);
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
            Name = string.IsNullOrWhiteSpace(name) ? null : name,
            View = view,
            Tab = tab,
            Close = close,
            Host = host
        };
        tab.Tag = slot;
        close.Tag = slot;
        tab.Checked += ExplorerTab_Checked;
        tab.MouseDoubleClick += ExplorerTab_DoubleClick;
        close.Click += CloseExplorer_Click;
        view.First.Navigated += _ => RefreshLabel(slot);
        view.Changed += () => RefreshLabel(slot);
        host.Children.Add(tab);
        host.Children.Add(close);
        _explorers.Add(slot);
        ExplorerTabs.Children.Add(host);
        RefreshClose();
        HostLog.Information(FXplorerCatalog.ExplorerOpened, "Lifecycle", "Explorer opened. " + (string.IsNullOrWhiteSpace(path) ? "New." : path));
        if (select)
            tab.IsChecked = true;
        else
            RefreshLabel(slot);
        FitTabs();
    }

    private void ExplorerTab_Checked(object sender, RoutedEventArgs e)
    {
        if (RootShell is null || sender is not RadioButton { Tag: ExplorerSlot slot })
            return;
        Show(slot);
        FitTabs();
    }

    private void ExplorerTab_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not RadioButton { Tag: ExplorerSlot slot })
            return;
        e.Handled = true;
        Dispatcher.BeginInvoke(() => OpenRename(slot), DispatcherPriority.Background);
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
            StaysOpen = true,
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
        popup.Child = MenuChrome(panel);
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
        box.LostKeyboardFocus += (_, _) => popup.IsOpen = false;
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
        FitTabs();
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
        var path = slot.Pane.Folder;
        _explorers.RemoveAt(index);
        ExplorerTabs.Children.Remove(slot.Host);
        RefreshClose();
        HostLog.Information(FXplorerCatalog.ExplorerClosed, "Lifecycle", "Explorer closed. " + (string.IsNullOrWhiteSpace(path) ? slot.Header : path));

        if (wasSelected)
        {
            var next = _explorers[Math.Min(index, _explorers.Count - 1)];
            next.Tab.IsChecked = true;
        }

        FitTabs();
    }

    private void Show(ExplorerSlot slot)
    {
        var item = RootShell["Explorer"];
        if (item is null)
            return;
        item.Content = slot.View;
        RootShell.SelectedItem = item;
    }

    private void RefreshClose()
    {
        var show = _explorers.Count > 1;
        foreach (var slot in _explorers)
            slot.Close.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TabColumn_SizeChanged(object sender, SizeChangedEventArgs e) => FitTabs();

    private void FitTabs()
    {
        if (_fitting || TabColumn is null || TabColumn.ActualWidth <= 0)
            return;

        _fitting = true;
        try
        {
            foreach (var slot in _explorers)
                slot.Host.Visibility = Visibility.Visible;

            var available = TabColumn.ActualWidth - AddExplorerButton.ActualWidth - 12;
            if (available < 40)
                available = TabColumn.ActualWidth;

            var hidden = new List<ExplorerSlot>();
            double used = 0;
            foreach (var slot in _explorers)
            {
                var width = TabWidth(slot);
                if (used + width <= available)
                    used += width;
                else
                    hidden.Add(slot);
            }

            var selected = _explorers.FirstOrDefault(slot => slot.Tab.IsChecked == true);
            if (selected is not null && hidden.Contains(selected))
            {
                hidden.Remove(selected);
                var visible = _explorers.Where(slot => !hidden.Contains(slot) && slot != selected).ToList();
                while (visible.Count > 0 && visible.Sum(TabWidth) + TabWidth(selected) > available)
                {
                    var drop = visible[^1];
                    visible.RemoveAt(visible.Count - 1);
                    hidden.Add(drop);
                }
            }

            foreach (var slot in hidden)
                slot.Host.Visibility = Visibility.Collapsed;

            _hidden.Clear();
            _hidden.AddRange(hidden);
            Overflow.Visibility = hidden.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            _fitting = false;
        }
    }

    private static double TabWidth(ExplorerSlot slot)
    {
        slot.Host.Measure(new Size(double.PositiveInfinity, 48));
        return slot.Host.DesiredSize.Width;
    }

    private void Overflow_Click(object sender, RoutedEventArgs e)
    {
        if (_hidden.Count == 0)
            return;

        var panel = new StackPanel { Margin = new Thickness(4) };
        foreach (var slot in _hidden)
        {
            var button = new Button
            {
                Content = slot.Tab.Content,
                Style = (Style)FindResource("Button.Utility"),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                MinWidth = 160,
                Margin = new Thickness(0, 2, 0, 2),
                Tag = slot
            };
            button.Click += OverflowItem_Click;
            panel.Children.Add(button);
        }

        var popup = new Popup
        {
            PlacementTarget = Overflow,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = MenuChrome(panel)
        };
        popup.IsOpen = true;
    }

    private void OverflowItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ExplorerSlot slot })
            return;
        if (sender is FrameworkElement element)
        {
            var popup = FindPopup(element);
            if (popup is not null)
                popup.IsOpen = false;
        }
        slot.Tab.IsChecked = true;
    }

    private static Popup? FindPopup(DependencyObject start)
    {
        DependencyObject? current = start;
        while (current is not null)
        {
            if (current is Popup popup)
                return popup;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private Border MenuChrome(UIElement child)
        => new()
        {
            Background = (System.Windows.Media.Brush)FindResource("Vestigium.Brushes.Surface.Window"),
            BorderBrush = (System.Windows.Media.Brush)FindResource("Vestigium.Brushes.Stroke.Default"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = child
        };

    private void MainNav_Checked(object sender, RoutedEventArgs e)
    {
        if (RootShell is null || sender is not RadioButton { Content: string header })
            return;

        var item = RootShell[header];
        if (item is not null)
            RootShell.SelectedItem = item;
    }
}
