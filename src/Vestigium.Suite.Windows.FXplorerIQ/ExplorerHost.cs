using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class ExplorerHost : DockPanel
{
    private readonly Grid _body = new();
    private readonly GridSplitter _split = new();
    private readonly StackPanel _jobBar;
    private readonly TextBlock _sourceText;
    private readonly TextBlock _destinationText;
    private readonly MenuItem _splitItem;
    private readonly MenuItem _verticalItem;
    private readonly MenuItem _horizontalItem;
    private readonly MenuItem _swapItem;
    private ScrollViewer? _viewport;
    private bool _applying;

    public ExplorerHost(Action<string> status)
    {
        LastChildFill = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ClipToBounds = true;
        First = new ExplorerPane { Status = status, FillCell = true, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        Second = new ExplorerPane { Status = status, FillCell = true, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        First.Navigated += _ => RefreshMarks();
        Second.Navigated += _ => RefreshMarks();
        Loaded += (_, _) => BindViewport();
        SizeChanged += (_, _) => Fit();

        _splitItem = Item("Split", Split_Click);
        _verticalItem = Item("Vertical", () => SetVertical(true));
        _horizontalItem = Item("Horizontal", () => SetVertical(false));
        _swapItem = Item("Swap source and destination", Swap_Click);
        var menu = new ContextMenu();
        menu.Items.Add(_splitItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(_verticalItem);
        menu.Items.Add(_horizontalItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(_swapItem);
        menu.Opened += (_, _) => RefreshMenu();
        ContextMenu = menu;
        PreviewMouseRightButtonUp += OpenMenu;

        _sourceText = Mark("Source");
        _destinationText = Mark("Destination");
        _jobBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(12, 8, 12, 0),
            Visibility = Visibility.Collapsed
        };
        _jobBar.Children.Add(_sourceText);
        _jobBar.Children.Add(_destinationText);
        Children.Add(_jobBar);
        DockPanel.SetDock(_jobBar, Dock.Top);

        _body.HorizontalAlignment = HorizontalAlignment.Stretch;
        _body.VerticalAlignment = VerticalAlignment.Stretch;
        _split.HorizontalAlignment = HorizontalAlignment.Stretch;
        _split.VerticalAlignment = VerticalAlignment.Stretch;
        _split.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
        _split.ShowsPreview = false;
        _split.DragCompleted += Split_DragCompleted;
        Children.Add(_body);

        Apply(false, App.Settings.Load());
    }

    public ExplorerPane First { get; }

    public ExplorerPane Second { get; }

    public bool IsSplit { get; private set; }

    public bool Vertical { get; private set; } = true;

    public double Ratio { get; private set; } = 0.5;

    public bool SourceIsFirst { get; private set; } = true;

    public string SourcePath => (SourceIsFirst ? First : Second).Folder;

    public string DestinationPath => (SourceIsFirst ? Second : First).Folder;

    public event Action? Changed;

    public void Apply(bool split, FXplorerSettings settings, string? secondPath = null, bool? sourceIsFirst = null)
    {
        _applying = true;
        try
        {
            Vertical = settings.SplitVertical;
            Ratio = settings.SplitRatio is > 0.1 and < 0.9 ? settings.SplitRatio : 0.5;
            IsSplit = split;
            if (sourceIsFirst is not null)
                SourceIsFirst = sourceIsFirst.Value;
            if (!string.IsNullOrWhiteSpace(secondPath))
                Second.Seed(secondPath);
            Layout();
            RefreshMarks();
            Fit();
        }
        finally
        {
            _applying = false;
        }
    }

    private void BindViewport()
    {
        _viewport = FindViewport();
        if (_viewport is not null)
            _viewport.SizeChanged += (_, _) => Fit();

        var window = Window.GetWindow(this);
        if (window is not null)
            window.SizeChanged += (_, _) => Fit();
        Fit();
    }

    private void Fit()
    {
        var width = _viewport?.ViewportWidth ?? 0;
        var height = _viewport?.ViewportHeight ?? 0;
        if (width < 100 || height < 100)
        {
            var window = Window.GetWindow(this);
            if (window is null)
                return;
            width = Math.Max(width, window.ActualWidth - 24);
            height = Math.Max(height, window.ActualHeight - 120);
        }

        if (width < 100 || height < 100)
            return;

        Width = width;
        Height = height;
        _body.Width = double.NaN;
        _body.Height = double.NaN;
    }

    private ScrollViewer? FindViewport()
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is ScrollViewer viewer)
                return viewer;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void OpenMenu(object sender, MouseButtonEventArgs e)
    {
        if (ContextMenu is null)
            return;
        RefreshMenu();
        ContextMenu.PlacementTarget = this;
        ContextMenu.IsOpen = true;
        e.Handled = true;
    }

    private void Split_Click()
    {
        IsSplit = !IsSplit;
        if (IsSplit && string.IsNullOrWhiteSpace(Second.Folder))
            Second.Seed(First.Folder);
        Layout();
        RefreshMarks();
        Save();
        Fit();
        HostLog.Information(IsSplit ? FXplorerCatalog.SplitOpened : FXplorerCatalog.SplitClosed, "Lifecycle", IsSplit ? "Split opened." : "Split closed.");
        Changed?.Invoke();
    }

    private void Swap_Click()
    {
        if (!IsSplit)
            return;
        SourceIsFirst = !SourceIsFirst;
        RefreshMarks();
        Changed?.Invoke();
    }

    private void RefreshMarks()
    {
        _jobBar.Visibility = IsSplit ? Visibility.Visible : Visibility.Collapsed;
        _sourceText.Text = "Source  " + OrNone(SourcePath);
        _destinationText.Text = "Destination  " + OrNone(DestinationPath);
    }

    private void RefreshMenu()
    {
        _splitItem.Header = IsSplit ? "Close split" : "Split";
        _verticalItem.IsEnabled = IsSplit;
        _horizontalItem.IsEnabled = IsSplit;
        _swapItem.IsEnabled = IsSplit;
        _verticalItem.IsChecked = IsSplit && Vertical;
        _horizontalItem.IsChecked = IsSplit && !Vertical;
    }

    private static string OrNone(string path) => string.IsNullOrWhiteSpace(path) ? "(none)" : path;

    private void SetVertical(bool vertical)
    {
        if (!IsSplit || Vertical == vertical)
            return;
        Vertical = vertical;
        Layout();
        Save();
        Fit();
        Changed?.Invoke();
    }

    private void Split_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        var total = Vertical ? _body.ActualWidth : _body.ActualHeight;
        if (total < 100)
            return;
        var first = Vertical ? _body.ColumnDefinitions[0].ActualWidth : _body.RowDefinitions[0].ActualHeight;
        var ratio = first / total;
        if (ratio is < 0.15 or > 0.85)
            return;
        Ratio = Math.Round(ratio, 3);
        Save();
        Changed?.Invoke();
    }

    private void Layout()
    {
        _body.Children.Clear();
        _body.RowDefinitions.Clear();
        _body.ColumnDefinitions.Clear();
        Grid.SetRow(First, 0);
        Grid.SetColumn(First, 0);

        if (!IsSplit)
        {
            _body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _body.Children.Add(First);
            return;
        }

        var first = new GridLength(Ratio, GridUnitType.Star);
        var second = new GridLength(1 - Ratio, GridUnitType.Star);
        if (Vertical)
        {
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = first, MinWidth = 0 });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = second, MinWidth = 0 });
            _body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _split.ResizeDirection = GridResizeDirection.Columns;
            _split.Width = 8;
            _split.Height = double.NaN;
            Grid.SetColumn(_split, 1);
            Grid.SetRow(_split, 0);
            Grid.SetColumn(Second, 2);
            Grid.SetRow(Second, 0);
        }
        else
        {
            _body.RowDefinitions.Add(new RowDefinition { Height = first, MinHeight = 0 });
            _body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            _body.RowDefinitions.Add(new RowDefinition { Height = second, MinHeight = 0 });
            _body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _split.ResizeDirection = GridResizeDirection.Rows;
            _split.Height = 8;
            _split.Width = double.NaN;
            Grid.SetRow(_split, 1);
            Grid.SetColumn(_split, 0);
            Grid.SetRow(Second, 2);
            Grid.SetColumn(Second, 0);
        }

        _body.Children.Add(First);
        _body.Children.Add(_split);
        _body.Children.Add(Second);
        if (string.IsNullOrWhiteSpace(Second.Folder))
            Second.Open(First.Folder);
    }

    private void Save()
    {
        if (_applying)
            return;
        var settings = App.Settings.Load();
        settings.SplitVertical = Vertical;
        settings.SplitRatio = Ratio;
        App.Settings.Save(settings);
    }

    private MenuItem Item(string text, Action click)
    {
        var item = new MenuItem { Header = text };
        item.Click += (_, _) => click();
        return item;
    }

    private TextBlock Mark(string caption)
        => new()
        {
            Text = caption,
            Style = (Style)Application.Current.FindResource("TextBlock.Caption"),
            Margin = new Thickness(0, 0, 24, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
}
