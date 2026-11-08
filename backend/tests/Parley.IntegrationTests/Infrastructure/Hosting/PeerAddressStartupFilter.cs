using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Parley.IntegrationTests.Infrastructure.Hosting;

/// <summary>
/// Gives every test request a socket address, which the test server does not set by default.
/// Runs first in the pipeline, so the address is in place for the rate limiter.
/// </summary>
public sealed class PeerAddressStartupFilter(IPAddress peerAddress) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            context.Connection.RemoteIpAddress ??= peerAddress;
            await nextMiddleware(context);
        });
        next(app);
    };
}
