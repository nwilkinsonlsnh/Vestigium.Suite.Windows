    public string HashAlgorithm { get; set; } = Vestigium.Helpers.Hashing.HashingAlgorithm.Sha256.ToString();

    public string Collision { get; set; } = "UniqueName";

    public string UniqueNamePattern { get; set; } = ".##";

    public bool AskBeforeOverwrite { get; set; } = true;

    public string VerifyAlgorithm { get; set; } = Vestigium.Helpers.Hashing.HashingAlgorithm.Sha256.ToString();