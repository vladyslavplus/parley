using System.Net;
using Microsoft.Extensions.Options;
using Parley.Api.Options;

namespace Parley.Api.Networking;

/// <summary>
/// Resolves the client address used for rate limiting. The socket address is the client unless the
/// socket belongs to a configured trusted proxy. From a trusted proxy the last <c>X-Forwarded-For</c>
/// entry is the client, because the proxy appends it. Forwarded headers from any other peer are ignored.
/// </summary>
/// <remarks>
/// Resolved by hand rather than with the forwarded-headers middleware: the middleware keeps trusting
/// its loopback defaults unless every default is cleared, and it applies headers only when the peer
/// matches its lists, which made the trust boundary hard to verify. Here the rule is one short method.
/// </remarks>
public sealed class ClientAddressResolver
{
    private const string ForwardedForHeader = "X-Forwarded-For";

    private const char ForwardedForSeparator = ',';

    private readonly HashSet<IPAddress> _trustedAddresses;

    private readonly IReadOnlyList<IPNetwork> _trustedNetworks;

    public ClientAddressResolver(IOptions<TrustedProxyOptions> options)
    {
        var settings = options.Value;
        _trustedAddresses = settings.KnownProxies
            .Select(proxy => Normalize(IPAddress.Parse(proxy)))
            .ToHashSet();
        _trustedNetworks = settings.KnownNetworks
            .Select(System.Net.IPNetwork.Parse)
            .ToArray();
    }

    /// <summary>The client address of the request, or null when the socket address is unknown.</summary>
    public IPAddress? Resolve(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var socketAddress = httpContext.Connection.RemoteIpAddress;
        if (socketAddress is null || !IsTrusted(Normalize(socketAddress)))
        {
            return socketAddress;
        }

        if (!httpContext.Request.Headers.TryGetValue(ForwardedForHeader, out var values))
        {
            return socketAddress;
        }

        // StringValues joins repeated header lines with commas; the last entry is the one the trusted proxy added.
        var lastEntry = values.ToString().Split(ForwardedForSeparator).Last().Trim();
        return IPAddress.TryParse(lastEntry, out var forwarded) ? Normalize(forwarded) : socketAddress;
    }

    private bool IsTrusted(IPAddress address) =>
        _trustedAddresses.Contains(address) || _trustedNetworks.Any(network => network.Contains(address));

    private static IPAddress Normalize(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}
