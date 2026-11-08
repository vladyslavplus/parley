using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parley.Identity.Domain.Users;
using Parley.Identity.Options.Auth;

namespace Parley.Identity.Authentication.Providers;

/// <summary>
/// Password reset token. Resetting the password rotates the security stamp, so the
/// token is single-use. Lifetime comes from <see cref="AuthOptions.PasswordResetTokenLifetime"/>.
/// </summary>
public sealed class PasswordResetTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<AuthOptions> authOptions,
    ILogger<DataProtectorTokenProvider<User>> logger)
    : DataProtectorTokenProvider<User>(
        dataProtectionProvider,
        Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions
        {
            Name = AuthConstants.PasswordResetTokenProvider,
            TokenLifespan = authOptions.Value.PasswordResetTokenLifetime,
        }),
        logger)
{
}
