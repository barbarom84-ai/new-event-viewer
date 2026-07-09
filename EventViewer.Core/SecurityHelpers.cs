using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace EventViewer.Core;

/// <summary>
/// Protects secrets at rest with Windows DPAPI (CurrentUser scope).
/// </summary>
public static class SecretProtector
{
    public static string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string protectedBase64)
    {
        if (string.IsNullOrWhiteSpace(protectedBase64))
        {
            return string.Empty;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(protectedBase64.Trim());
            var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            return string.Empty;
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// Validates optional AI cloud endpoints to reduce SSRF / cleartext risks.
/// </summary>
public static class AiEndpointPolicy
{
    public static bool IsAllowed(string? endpoint)
        => TryValidate(endpoint, out _, out _);

    public static bool TryValidate(string? endpoint, out Uri? uri, out string error)
    {
        uri = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            error = "Endpoint vide.";
            return false;
        }

        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var parsed))
        {
            error = "URL invalide.";
            return false;
        }

        if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            error = "Seul HTTPS est autorisé.";
            return false;
        }

        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            error = "Identifiants dans l'URL interdits.";
            return false;
        }

        var host = parsed.DnsSafeHost;
        if (string.IsNullOrWhiteSpace(host) ||
            string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            error = "Hôte local interdit.";
            return false;
        }

        if (IPAddress.TryParse(host, out var ip) && IsDisallowedAddress(ip))
        {
            error = "Adresse IP privée / de boucle interdite.";
            return false;
        }

        uri = parsed;
        return true;
    }

    private static bool IsDisallowedAddress(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            return bytes[0] switch
            {
                0 => true,
                10 => true,
                127 => true,
                169 when bytes[1] == 254 => true,
                172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false
            };
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast)
            {
                return true;
            }

            var bytes = ip.GetAddressBytes();
            // Unique local fc00::/7
            if ((bytes[0] & 0xfe) == 0xfc)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Safe file reads for settings / snapshots.
/// </summary>
public static class SafeFileIO
{
    public const long DefaultMaxBytes = 2 * 1024 * 1024; // 2 MB

    public static string? ReadAllTextLimited(string path, long maxBytes = DefaultMaxBytes)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0)
        {
            return null;
        }

        if (info.Length > maxBytes)
        {
            throw new InvalidDataException($"Fichier trop volumineux ({info.Length} octets).");
        }

        return File.ReadAllText(path);
    }
}
