using System.Globalization;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Parley.Api.Controllers.Auth.Requests;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Api.RateLimiting;
using Parley.Identity.Authentication.Tokens;
using Parley.Identity.Features.Accounts.ConfirmEmail;
using Parley.Identity.Features.Accounts.ForgotPassword;
using Parley.Identity.Features.Accounts.Register;
using Parley.Identity.Features.Accounts.ResendConfirmation;
using Parley.Identity.Features.Accounts.ResetPassword;
using Parley.Identity.Features.Sessions.Refresh;
using Parley.Identity.Features.Sessions.SignIn;
using Parley.Identity.Features.Sessions.SignOut;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Auth;


/// <summary>
/// Account lifecycle and session endpoints under <c>/auth</c>. Each action binds the
/// body, passes the request culture explicitly, sends one mediator request and maps
/// the result. No business rules live here.
/// </summary>
[Route(AuthRoutes.Base)]
public sealed class AuthController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpPost(AuthRoutes.Register)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(AuthRateLimitPolicies.Register)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RegisterAccountCommand(
                request?.Email ?? string.Empty,
                request?.Password ?? string.Empty,
                request?.OrganizationName ?? string.Empty,
                RequestCulture()),
            cancellationToken);
        return result.IsSuccess ? Accepted() : Problem(result.Error!);
    }

    [HttpPost(AuthRoutes.ConfirmEmail)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(AuthRateLimitPolicies.ConfirmEmail)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ConfirmEmailCommand(request?.UserId ?? Guid.Empty, request?.Token ?? string.Empty),
            cancellationToken);
        return FromResult(result);
    }

    [HttpPost(AuthRoutes.ResendConfirmation)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(AuthRateLimitPolicies.ResendConfirmation)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendConfirmation([FromBody] EmailRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ResendConfirmationCommand(request?.Email ?? string.Empty, RequestCulture()),
            cancellationToken);
        return result.IsSuccess ? Accepted() : Problem(result.Error!);
    }

    [HttpPost(AuthRoutes.SignIn)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(AuthRateLimitPolicies.Login)]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SignInCommand(request?.Email ?? string.Empty, request?.Password ?? string.Empty),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(AuthRoutes.Refresh)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(AuthRateLimitPolicies.Refresh)]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RefreshSessionCommand(request?.RefreshToken ?? string.Empty), cancellationToken);
        return FromResult(result, Ok);
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [HttpPost(AuthRoutes.SignOut)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    public async Task<IActionResult> SignOut([FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SignOutCommand(request?.RefreshToken ?? string.Empty), cancellationToken);
        return FromResult(result);
    }

    [HttpPost(AuthRoutes.ForgotPassword)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(AuthRateLimitPolicies.ForgotPassword)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword([FromBody] EmailRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ForgotPasswordCommand(request?.Email ?? string.Empty, RequestCulture()),
            cancellationToken);
        return result.IsSuccess ? Accepted() : Problem(result.Error!);
    }

    [HttpPost(AuthRoutes.ResetPassword)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(AuthRateLimitPolicies.ResetPassword)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ResetPasswordCommand(
                request?.UserId ?? Guid.Empty,
                request?.Token ?? string.Empty,
                request?.NewPassword ?? string.Empty),
            cancellationToken);
        return FromResult(result);
    }

    /// <summary>Two-letter language of the request, set by request localization from Accept-Language.</summary>
    private static string RequestCulture() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
}
