using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.ForgotPassword;

/// <summary>
/// Queues a password reset email for a confirmed account. The result is the same for
/// known and unknown addresses, and the caller gets no hint about which one applied.
/// </summary>
/// <param name="Email">Address to send the reset link to.</param>
/// <param name="Culture">Request culture, used as the email language.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record ForgotPasswordCommand(string Email, string Culture) : IRequest<Result>;
