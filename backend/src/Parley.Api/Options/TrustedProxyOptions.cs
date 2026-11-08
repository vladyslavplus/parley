namespace Parley.Api.Options;

/// <summary>
/// Reverse proxies whose <c>X-Forwarded-For</c> header is trusted. Empty by default, so the
/// socket address is the client address. Set this when a front-end server (for example the
/// web BFF) sits between clients and the API. Overridable by environment variables such as
/// <c>TrustedProxies__KnownProxies__0</c>.
/// </summary>
public sealed class TrustedProxyOptions
{
    public const string SectionName = "TrustedProxies";

    /// <summary>Single addresses of trusted proxies, for example <c>10.0.0.5</c>.</summary>
    public string[] KnownProxies { get; set; } = [];

    /// <summary>Trusted proxy networks in CIDR notation, for example <c>10.0.0.0/8</c>.</summary>
    public string[] KnownNetworks { get; set; } = [];
}
