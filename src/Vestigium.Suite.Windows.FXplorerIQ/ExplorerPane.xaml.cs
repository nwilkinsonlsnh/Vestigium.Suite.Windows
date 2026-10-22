    private void OnSettingsChanged()
    {
        if (_applying)
            return;
        Dispatcher.Invoke(() =>
        {
            var settings = App.Settings.Load();
            var algorithm = settings.Algorithm().ToString();
            ApplyLayout(settings);
            LoadRoots();
            if (_folder.Length > 0)
                Open(_folder);
            else if (!string.Equals(algorithm, _algorithm, StringComparison.OrdinalIgnoreCase))
                Open(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        });
    }