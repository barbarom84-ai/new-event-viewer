using System.Text;
using System.Text.RegularExpressions;

namespace EventViewer.Core;

/// <summary>
/// Turns raw Event Log text (often the useless "description cannot be found" stub)
/// into a readable technical summary for the UI.
/// </summary>
public static partial class EventDetailFormatter
{
    [GeneratedRegex(
        @"The following information is part of the event:\s*(?<payload>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex InsertionPayloadRegex();

    [GeneratedRegex(
        @"Les informations suivantes font partie de l['’]événement\s*:\s*(?<payload>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex InsertionPayloadFrRegex();

    [GeneratedRegex(@"'(?:''|[^'])*'|""(?:[^""]|"""")*""", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedTokenRegex();

    public static string FormatTechnicalDetail(int eventId, string? source, string? rawMessage)
    {
        var raw = rawMessage ?? string.Empty;
        var insertions = ExtractInsertions(raw);
        var isMissingDescription = IsMissingDescriptionMessage(raw);

        if (eventId == 10016 || TagDetector.IsDcom(source, raw))
        {
            var dcom = FormatDcom10016(source, raw, insertions, isMissingDescription);
            if (!string.IsNullOrWhiteSpace(dcom))
            {
                return dcom;
            }
        }

        if (isMissingDescription && insertions.Count > 0)
        {
            return FormatGenericMissingDescription(eventId, source, insertions);
        }

        return string.IsNullOrWhiteSpace(raw) ? Loc.T("TechDetail.None") : raw.Trim();
    }

    public static bool IsMissingDescriptionMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("cannot be found", StringComparison.OrdinalIgnoreCase)
               || message.Contains("n'a pas été trouvée", StringComparison.OrdinalIgnoreCase)
               || message.Contains("n’a pas été trouvée", StringComparison.OrdinalIgnoreCase)
               || message.Contains("message DLL", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> ExtractInsertions(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return [];
        }

        var match = InsertionPayloadRegex().Match(message);
        if (!match.Success)
        {
            match = InsertionPayloadFrRegex().Match(message);
        }

        if (!match.Success)
        {
            // Fallback: collect all quoted tokens in the whole message.
            return ParseQuotedTokens(message);
        }

        return ParseQuotedTokens(match.Groups["payload"].Value);
    }

    private static IReadOnlyList<string> ParseQuotedTokens(string payload)
    {
        var list = new List<string>();
        foreach (Match m in QuotedTokenRegex().Matches(payload))
        {
            var token = m.Value;
            if (token.Length >= 2)
            {
                token = token[1..^1]
                    .Replace("''", "'", StringComparison.Ordinal)
                    .Replace("\"\"", "\"", StringComparison.Ordinal);
            }

            list.Add(token.Trim());
        }

        return list;
    }

    private static string FormatDcom10016(
        string? source,
        string raw,
        IReadOnlyList<string> insertions,
        bool isMissingDescription)
    {
        // Classic DCOM 10016 insertion order (Microsoft):
        // 0 permission scope, 1 Local/Remote, 2 Activation/Launch,
        // 3 CLSID, 4 AppID, 5 account/computer, 6 user, 7 SID,
        // 8 address, 9 app container, 10 app container SID
        string Get(int i) => i < insertions.Count ? insertions[i] : string.Empty;

        var permission = Get(0);
        var scope = Get(1);
        var access = Get(2);
        var clsid = NormalizeGuidToken(Get(3));
        var appId = NormalizeGuidToken(Get(4));
        var account = Get(5);
        var user = Get(6);
        var sid = Get(7);
        var address = Get(8);
        var appContainer = Get(9);
        var appContainerSid = Get(10);

        // If quoted parse failed, still try GUIDs from the raw blob (CLSID then AppID).
        var guids = ComComponentResolver.ExtractGuids(raw)
            .Where(g => !IsNullGuid(g))
            .ToList();
        if (string.IsNullOrWhiteSpace(clsid) && guids.Count > 0)
        {
            clsid = guids[0];
        }

        if (string.IsNullOrWhiteSpace(appId) && guids.Count > 1)
        {
            appId = guids[1];
        }

        var clsidInfo = string.IsNullOrWhiteSpace(clsid) ? null : ComComponentResolver.ResolveGuid(clsid);
        var appIdInfo = string.IsNullOrWhiteSpace(appId) ? null : ComComponentResolver.ResolveGuid(appId);

        var sb = new StringBuilder();
        sb.AppendLine(Loc.T("TechDetail.Dcom.Title"));
        sb.AppendLine("────────────────────────────");
        sb.AppendLine(Loc.T("TechDetail.Dcom.MissingDll"));
        sb.AppendLine(Loc.T("TechDetail.Dcom.ExtractedIntro"));
        sb.AppendLine();

        AppendLine(sb, Loc.T("TechDetail.Label.Source"), string.IsNullOrWhiteSpace(source) ? "DCOM" : source);
        AppendLine(sb, Loc.T("TechDetail.Label.Code"), "10016");
        AppendLine(sb, Loc.T("TechDetail.Label.PermissionType"), HumanizePermission(permission));
        AppendLine(sb, Loc.T("TechDetail.Label.Scope"), HumanizeOrDash(scope));
        AppendLine(sb, Loc.T("TechDetail.Label.Operation"), HumanizeAccess(access));
        sb.AppendLine();

        AppendLine(sb, Loc.T("TechDetail.Label.ComponentClsid"), FormatGuidLine(clsid, clsidInfo));
        AppendLine(sb, Loc.T("TechDetail.Label.ApplicationAppId"), FormatGuidLine(appId, appIdInfo));
        sb.AppendLine();

        AppendLine(sb, Loc.T("TechDetail.Label.Account"), HumanizeOrDash(account));
        AppendLine(sb, Loc.T("TechDetail.Label.User"), HumanizeOrDash(user));
        AppendLine(sb, Loc.T("TechDetail.Label.Sid"), HumanizeOrDash(sid));
        AppendLine(sb, Loc.T("TechDetail.Label.Address"), HumanizeOrDash(address));
        AppendLine(sb, Loc.T("TechDetail.Label.AppContainer"), HumanizeOrDash(appContainer));
        if (!string.IsNullOrWhiteSpace(appContainerSid) &&
            !appContainerSid.Equals(Loc.T("TechDetail.Unavailable"), StringComparison.OrdinalIgnoreCase) &&
            !appContainerSid.Equals("Non disponible", StringComparison.OrdinalIgnoreCase) &&
            !appContainerSid.Equals("Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            AppendLine(sb, Loc.T("TechDetail.Label.ContainerSid"), appContainerSid);
        }

        sb.AppendLine();
        sb.AppendLine(Loc.T("TechDetail.Dcom.BenignNote"));

        if (!isMissingDescription && !string.IsNullOrWhiteSpace(raw) && insertions.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine(Loc.T("TechDetail.RawMessage"));
            sb.AppendLine(raw.Trim());
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatGenericMissingDescription(
        int eventId,
        string? source,
        IReadOnlyList<string> insertions)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Loc.T("TechDetail.Generic.Title"));
        sb.AppendLine("────────────────");
        sb.AppendLine(Loc.T("TechDetail.Generic.MissingDll"));
        sb.AppendLine(Loc.T("TechDetail.Generic.SourceCode", source ?? "?", eventId));
        sb.AppendLine();
        sb.AppendLine(Loc.T("TechDetail.Generic.EventData"));

        for (var i = 0; i < insertions.Count; i++)
        {
            var value = insertions[i];
            if (ComComponentResolver.ExtractGuids(value).Count > 0)
            {
                var guid = ComComponentResolver.ExtractGuids(value)[0];
                var info = ComComponentResolver.ResolveGuid(guid);
                sb.AppendLine($"  • {FormatGuidLine(guid, info)}");
            }
            else
            {
                sb.AppendLine($"  • {HumanizeOrDash(value)}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string NormalizeGuidToken(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return string.Empty;
        }

        return ComComponentResolver.ExtractGuids(candidate).FirstOrDefault() ?? string.Empty;
    }

    private static bool IsNullGuid(string guid)
        => string.Equals(guid, "{00000000-0000-0000-0000-000000000000}", StringComparison.OrdinalIgnoreCase);

    private static string FormatGuidLine(string? guid, ComComponentInfo? info)
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            return "—";
        }

        if (info != null && !string.IsNullOrWhiteSpace(info.DisplayName) &&
            !info.DisplayName.Equals("Composant COM inconnu", StringComparison.Ordinal))
        {
            return $"{info.DisplayName} · {guid}";
        }

        return guid;
    }

    private static void AppendLine(StringBuilder sb, string label, string value)
        => sb.AppendLine($"{label} : {value}");

    private static string HumanizeOrDash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Equals("Non disponible", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Unavailable", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            return "—";
        }

        return value.Trim();
    }

    private static string HumanizePermission(string? value)
    {
        var v = HumanizeOrDash(value);
        if (v == "—")
        {
            return v;
        }

        if (v.Contains("application", StringComparison.OrdinalIgnoreCase) ||
            v.Contains("propres", StringComparison.OrdinalIgnoreCase))
        {
            return Loc.T("TechDetail.Permission.Application");
        }

        return v;
    }

    private static string HumanizeAccess(string? value)
    {
        var v = HumanizeOrDash(value);
        if (v.Equals("Activation", StringComparison.OrdinalIgnoreCase))
        {
            return Loc.T("TechDetail.Access.Activation");
        }

        if (v.Equals("Launch", StringComparison.OrdinalIgnoreCase) ||
            v.Contains("lancement", StringComparison.OrdinalIgnoreCase))
        {
            return Loc.T("TechDetail.Access.Launch");
        }

        return v;
    }
}
