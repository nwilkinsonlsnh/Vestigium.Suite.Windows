    public Action<string>? Status { get; set; }

    public string Folder => _folder;

    public event Action<string>? Navigated;