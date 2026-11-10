using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Parley.Api.Localization;
using Parley.Api.Networking;
using Parley.Campaigns.Options;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Options.RateLimiting;
using Parley.SharedKernel.Results;

namespace Parley.Api.RateLimiting;

/// <summary>
/// Fixed-window limits per endpoint, partitioned by client address. Rejections are
/// written as the shared ProblemDetails shape with status 429, so clients branch on
/// the same <c>code</c> field as for every other failure.
/// </summary>
public static class AuthRateLimiting
{
    private const string ProblemJsonContentType = "application/problem+json";

    private const int QueueLimit = 0;

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ClientAddressResolver>();
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = WriteRejectionAsync;

            AddPolicy(limiter, AuthRateLimitPolicies.Register, options => options.Register);
            AddPolicy(limiter, AuthRateLimitPolicies.Login, options => options.Login);
            AddPolicy(limiter, AuthRateLimitPolicies.Refresh, options => options.Refresh);
            AddPolicy(limiter, AuthRateLimitPolicies.ConfirmEmail, options => options.ConfirmEmail);
            AddPolicy(limiter, AuthRateLimitPolicies.ResendConfirmation, options => options.ResendConfirmation);
            AddPolicy(limiter, AuthRateLimitPolicies.ForgotPassword, options => options.ForgotPassword);
            AddPolicy(limiter, AuthRateLimitPolicies.ResetPassword, options => options.ResetPassword);
            AddPolicy(limiter, AuthRateLimitPolicies.InvitationCreate, options => options.InvitationCreate);
            AddPolicy(limiter, AuthRateLimitPolicies.InvitationAccept, options => options.InvitationAccept);
            AddUnsubscribePolicy(limiter);
        });

        return services;
    }

    /// <summary>Unsubscribe links are public and carry no sign-in, so they are limited per client address like the auth endpoints.</summary>
    private static void AddUnsubscribePolicy(RateLimiterOptions limiter)
    {
        limiter.AddPolicy<string>(CampaignsRateLimitPolicies.Unsubscribe, httpContext =>
        {
            var settings = httpContext.RequestServices.GetRequiredService<IOptions<CampaignsSendOptions>>().Value;
            var resolver = httpContext.RequestServices.GetRequiredService<ClientAddressResolver>();
            var address = resolver.Resolve(httpContext)?.ToString() ?? AuthRateLimitPolicies.UnknownClientAddress;
            return RateLimitPartition.GetFixedWindowLimiter(
                $"{CampaignsRateLimitPolicies.Unsubscribe}:{address}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.UnsubscribePermitLimit,
                    Window = settings.UnsubscribeWindow,
                    QueueLimit = QueueLimit,
                    AutoReplenishment = true,
                });
        });
    }

    private static void AddPolicy(
        RateLimiterOptions limiter,
        string policyName,
        Func<RateLimitingOptions, FixedWindowLimitOptions> select)
    {
        limiter.AddPolicy<string>(policyName, httpContext =>
        {
            var settings = select(httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value);
            var resolver = httpContext.RequestServices.GetRequiredService<ClientAddressResolver>();
            var address = resolver.Resolve(httpContext)?.ToString() ?? AuthRateLimitPolicies.UnknownClientAddress;
            return RateLimitPartition.GetFixedWindowLimiter(
                $"{policyName}:{address}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = settings.Window,
                    QueueLimit = QueueLimit,
                    AutoReplenishment = true,
                });
        });
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var errorTexts = httpContext.RequestServices.GetRequiredService<ErrorTextLocalizer>();
        var error = Error.Failure(IdentityErrorCodes.AuthRateLimited);
        var detail = errorTexts.Get(error);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            httpContext.Response.Headers[HeaderNames.RetryAfter] = seconds.ToString(CultureInfo.InvariantCulture);
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = detail,
            Detail = detail,
            Extensions =
            {
                [ApiConstants.ProblemCode] = error.Code,
                [ApiConstants.ProblemTraceId] = httpContext.TraceIdentifier,
            },
        };

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var jsonOptions = httpContext.RequestServices.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions;
        await httpContext.Response.WriteAsJsonAsync(problem, jsonOptions, ProblemJsonContentType, cancellationToken);
    }
}
