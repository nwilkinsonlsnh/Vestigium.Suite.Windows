using System.Text.RegularExpressions;
using Vestigium.Helpers.Kql;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

internal static class KqlHost
{
    private static readonly Regex Structured = new(
        @"(==|!=|<>|<=|>=|\sAND\s|\sOR\s|\sNOT\s|\sLIKE\s|\sCONTAINS\s|\sBETWEEN\s|\sIN\s|\.)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static KqlBoundQuery? Compile(string? text, KqlSession session, KqlBoundQuery? previous, out string status)
    {
        if (string.IsNullOrWhiteSpace(text) || !HasCriteria(text, session))
        {
            status = string.Empty;
            return null;
        }

        var compiled = KqlHelper.Compile(Rewrite(text, session), session);
        if (!compiled.Ok)
        {
            status = compiled.Error?.Message ?? "query failed";
            return null;
        }

        status = string.Empty;
        return compiled.Query;
    }

    private static bool HasCriteria(string text, KqlSession session)
    {
        var body = text.Trim();
        if (body.Length == 0)
            return false;
        var open = body.IndexOf('(');
        if (open >= 0)
            return body.IndexOf(')', open + 1) > open + 1;
        if (body.Any(ch => ch is '=' or '!' or '<' or '>') || Structured.IsMatch(body) && body.Contains(' '))
            return true;
        var token = body.TrimEnd('.').Trim();
        if (token.Length == 0)
            return false;
        if (session.Fields.Any(field =>
                field.Canonical.StartsWith(token, StringComparison.OrdinalIgnoreCase)
                || field.Aliases.Any(alias => alias.StartsWith(token, StringComparison.OrdinalIgnoreCase))))
            return false;
        return !body.Contains('.');
    }

    private static string Rewrite(string text, KqlSession session)
    {
        var body = text.Trim().Trim('"');
        if (body.Length == 0 || Structured.IsMatch(text))
            return text;

        var token = body.TrimEnd('.').Trim();
        if (session.Fields.Any(field =>
                field.Canonical.StartsWith(token, StringComparison.OrdinalIgnoreCase)
                || field.Aliases.Any(alias => alias.StartsWith(token, StringComparison.OrdinalIgnoreCase))))
            return text;
        var fields = session.Fields.Where(field => field.Type == KqlType.String).Select(field => field.Canonical).ToArray();
        if (fields.Length == 0)
            return text;

        var value = body.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return string.Join(" || ", fields.Select(field => $"{field} CONTAINS \"{value}\""));
    }
}
