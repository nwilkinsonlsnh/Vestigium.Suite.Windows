    public string ExcludeFileMasks { get; set; } = "";

    public string ExcludeDirectoryMasks { get; set; } = "";

    public long? MinSizeBytes { get; set; }

    public long? MaxSizeBytes { get; set; }

    public int? MinAgeDays { get; set; }

    public int? MaxAgeDays { get; set; }

    public int? MaxDepth { get; set; }

    public bool IncludeEmptyDirectories { get; set; }

    public bool CopyTimestamps { get; set; } = true;

    public int RetryCount { get; set; } = 3;

    public int RetryWaitSeconds { get; set; } = 2;