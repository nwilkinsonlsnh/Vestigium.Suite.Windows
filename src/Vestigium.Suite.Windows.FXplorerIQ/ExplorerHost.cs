    private static FileIoJobOptions Options(FXplorerSettings settings, FileIoCollision collision)
        => new()
        {
            Collision = collision,
            UniqueNamePattern = string.IsNullOrWhiteSpace(settings.UniqueNamePattern) ? ".##" : settings.UniqueNamePattern,
            Purge = false,
            HashAlgorithm = settings.Verify(),
            ReconLeadTime = TimeSpan.FromSeconds(2),
            ExcludeFileMasks = Split(settings.ExcludeFileMasks),
            ExcludeDirectoryMasks = Split(settings.ExcludeDirectoryMasks),
            MinSizeBytes = settings.MinSizeBytes,
            MaxSizeBytes = settings.MaxSizeBytes,
            MinAge = settings.MinAgeDays is { } min ? new FileIoAge { Days = min } : null,
            MaxAge = settings.MaxAgeDays is { } max ? new FileIoAge { Days = max } : null,
            MaxDepth = settings.MaxDepth,
            IncludeEmptyDirectories = settings.IncludeEmptyDirectories,
            CopyTimestampsAndAttributes = settings.CopyTimestamps,
            RetryCount = settings.RetryCount,
            RetryWait = TimeSpan.FromSeconds(settings.RetryWaitSeconds)
        };

    private static string[] Split(string masks)
        => string.IsNullOrWhiteSpace(masks)
            ? []
            : masks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);