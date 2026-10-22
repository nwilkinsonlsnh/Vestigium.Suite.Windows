    public void Seed(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
            _folder = path;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_roots.Count == 0)
            LoadRoots();
        Open(_folder.Length == 0
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : _folder);