using Vestigium.Helpers.Kql;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

internal static class KqlHost
{
    public static KqlBoundQuery? Compile(string? text, KqlSession session, KqlBoundQuery? previous, out string status)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            status = string.Empty;
            return null;
        }

        var compiled = KqlHelper.Compile(text, session);
        if (!compiled.Ok)
        {
            status = compiled.Error?.Message ?? "query failed";
            return previous;
        }

        status = string.Empty;
        return compiled.Query;
    }
}
