using System.Windows;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public enum ElevationChoice
{
    Cancelled,
    Accepted,
}

public static class JobHandoff
{
    public static ElevationChoice Offer(Window? owner, JobVerb verb, string source, string? destination, bool secure, bool audit)
    {
        var path = string.IsNullOrWhiteSpace(destination) ? source : destination;
        var verbName = audit ? "Audit" : Label(verb, secure);
        if (!ElevationPrompt.Ask(owner, verbName, path))
            return ElevationChoice.Cancelled;

        return ElevationChoice.Accepted;
    }

    private static string Label(JobVerb verb, bool secure)
        => verb switch
        {
            JobVerb.Delete => secure ? "Delete (Secure)" : "Delete",
            JobVerb.Move => "Move",
            JobVerb.Mirror => "Mirror (Verify)",
            _ => "Copy"
        };
}
