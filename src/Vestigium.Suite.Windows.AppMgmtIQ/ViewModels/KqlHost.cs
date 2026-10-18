using Vestigium.Helpers.Kql;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

internal static class KqlHost
{
    public static KqlBoundQuery? Compile(string? text, KqlSession session, KqlBoundQuery? previous, out string status)
    {
        var prepared = KqlPrepare.Run(text, session);
        status = prepared.Message;
        return prepared.State == KqlPrepareState.Ready ? prepared.Query : null;
    }
}
