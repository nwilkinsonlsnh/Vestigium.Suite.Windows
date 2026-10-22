    public string HashAlgorithm { get; set; } = Vestigium.Helpers.Hashing.HashingAlgorithm.Sha256.ToString();

    public bool HideEmptyDrives { get; set; }

    public bool ShowAllFolders { get; set; }

    public bool ShowHiddenFiles { get; set; } = true;

    public bool ShowFileExtensions { get; set; } = true;

    public bool ShowSystemFiles { get; set; }

    public bool HideJunctions { get; set; } = true;