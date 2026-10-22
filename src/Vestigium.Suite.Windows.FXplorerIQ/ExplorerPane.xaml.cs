    private void TreeToggle_Click(object sender, RoutedEventArgs e)
    {
        _treeOpen = !_treeOpen;
        var open = App.Settings.Load().TreeWidth;
        if (open < 160)
            open = FXplorerSettings.DefaultTreeWidth;

        TreeColumn.Width = new GridLength(_treeOpen ? open : ClosedWidth);
        TreeColumn.MinWidth = _treeOpen ? 160 : 0;
        TreeHost.Visibility = _treeOpen ? Visibility.Visible : Visibility.Collapsed;
        TreeOpenHost.Visibility = _treeOpen ? Visibility.Collapsed : Visibility.Visible;
        TreeSplit.Visibility = _treeOpen ? Visibility.Visible : Visibility.Collapsed;
        SplitColumn.Width = new GridLength(_treeOpen ? 8 : 0);
    }