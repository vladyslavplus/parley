using System.Net;
using Microsoft.Extensions.Options;

namespace Parley.Api.Options;

/// <summary>Startup validation for <see cref="TrustedProxyOptions"/>.</summary>
public sealed class TrustedProxyOptionsValidator : IValidateOptions<TrustedProxyOptions>
{
    public ValidateOptionsResult Validate(string? name, TrustedProxyOptions options)
    {
        if (options.KnownProxies.Any(proxy => !IPAddress.TryParse(proxy, out _)))
        {
            return ValidateOptionsResult.Fail("Trusted proxy addresses must be valid IP addresses.");
        }

        if (options.KnownNetworks.Any(network => !IPNetwork.TryParse(network, out _)))
        {
            return ValidateOptionsResult.Fail("Trusted proxy networks must be valid CIDR ranges.");
        }

        return ValidateOptionsResult.Success;
    }
}
