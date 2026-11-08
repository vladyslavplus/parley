using Scalar.AspNetCore;

namespace Parley.Api.Startup;

public static class DevelopmentEndpointExtensions
{
    /// <summary>
    /// Maps the endpoints that exist only in the development environment.
    /// </summary>
    public static void MapDevelopmentEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }
    }
}
