    public Action<string>? Status { get; set; }

    public string Folder => _folder;

    public bool FillCell { get; set; }

    public event Action<string>? Navigated;