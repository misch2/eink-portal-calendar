using System.Net;
using System.Net.Sockets;

namespace PortalCalendarServer.Validation;

public static class UrlValidator
{
    /// <summary>
    /// Validates that a URL is safe to fetch: must be http/https and must not resolve
    /// to a loopback, link-local, or RFC 1918 private address (SSRF prevention).
    /// </summary>
    public static async Task ValidateUrlIsSafe(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Invalid URL: {url}");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"URL scheme must be http or https, got: {uri.Scheme}");

        var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost);
        if (addresses.Length == 0)
            throw new InvalidOperationException($"URL hostname could not be resolved: {uri.DnsSafeHost}");

        foreach (var address in addresses)
        {
            if (IsPrivateOrReservedAddress(address))
                throw new InvalidOperationException(
                    $"URL '{url}' resolves to a private or reserved address ({address}) and cannot be fetched");
        }
    }

    private static bool IsPrivateOrReservedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Link-local IPv6: fe80::/10
            var b = address.GetAddressBytes();
            return b[0] == 0xfe && (b[1] & 0xc0) == 0x80;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] == 10 ||                             // 10.0.0.0/8
                   (b[0] == 172 && (b[1] & 0xf0) == 16) || // 172.16.0.0/12
                   (b[0] == 192 && b[1] == 168) ||          // 192.168.0.0/16
                   (b[0] == 169 && b[1] == 254);            // 169.254.0.0/16 link-local
        }

        return false;
    }
}
