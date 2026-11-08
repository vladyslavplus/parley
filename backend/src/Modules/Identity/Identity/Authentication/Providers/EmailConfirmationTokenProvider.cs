using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parley.Identity.Domain.Users;
using Parley.Identity.Options.Auth;

namespace Parley.Identity.Authentication.Providers;

/// <summary>
/// Single-use email confirmation token. Identity tokens embed the security stamp,
/// and confirming the email rotates that stamp, so a second use fails. Lifetime
/// comes from <see cref="AuthOptions.EmailConfirmationTokenLifetime"/>.
/// </summary>
public sealed class EmailConfirmationTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<AuthOptions> authOptions,
    ILogger<DataProtectorTokenProvider<User>> logger)
    : DataProtectorTokenProvider<User>(
        dataProtectionProvider,
        Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions
        {
            Name = AuthConstants.EmailConfirmationTokenProvider,
            TokenLifespan = authOptions.Value.EmailConfirmationTokenLifetime,
        }),
        logger)
{
}
