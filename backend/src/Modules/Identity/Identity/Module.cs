using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Parley.Identity.Authentication;
using Parley.Identity.Authentication.Passwords;
using Parley.Identity.Authentication.Providers;
using Parley.Identity.Authentication.Refresh;
using Parley.Identity.Authentication.Tokens;
using Parley.Identity.Authorization.Permissions;
using Parley.Identity.Authorization.Scope;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Contracts.DemoSeed;
using Parley.Identity.Contracts.Organizations;
using Parley.Identity.Contracts.Users;
using Parley.Identity.Directory;
using Parley.Identity.Domain.Users;
using Parley.Identity.Email;
using Parley.Identity.Email.Jobs;
using Parley.Identity.Email.Links;
using Parley.Identity.Features.Accounts.Register;
using Parley.Identity.Features.Audit.Consumers;
using Parley.Identity.Options.Auth;
using Parley.Identity.Options.Authorization;
using Parley.Identity.Options.Invitations;
using Parley.Identity.Options.RateLimiting;
using Parley.Identity.Options.Storage;
using Parley.Identity.Persistence;
using Parley.Identity.Seeding.DemoSeed;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Persistence.Interceptors;

namespace Parley.Identity;

/// <summary>
/// Identity module boundary and registration: account lifecycle, sign-in and token
/// rotation, memberships and authorization, the identity schema, and the
/// access-token validation scheme.
/// </summary>
public static class Module
{
    private const int PasswordRequiredUniqueCharacters = 1;

    /// <summary>
    /// Registers the identity module. Options bind from the <c>Auth</c>,
    /// <c>RateLimiting</c>, <c>IdentityStorage</c> and <c>Authorization</c> sections and
    /// are validated on start. The caller registers Notifications (email), tenancy and
    /// Hangfire separately.
    /// </summary>
    public static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();

        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitingOptions>, RateLimitingOptionsValidator>();

        services.AddOptions<IdentityStorageOptions>()
            .Bind(configuration.GetSection(IdentityStorageOptions.SectionName));

        services.AddOptions<AuthorizationOptions>()
            .Bind(configuration.GetSection(AuthorizationOptions.SectionName))
            .Validate(options => options.RolePermissionCacheLifetime > TimeSpan.Zero, "Role permission cache lifetime must be positive.")
            .ValidateOnStart();

        services.AddDbContext<IdentityDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(provider.GetRequiredService<AuditStampingInterceptor>()));

        services.AddOptions<IdentityOptions>()
            .Configure<IOptions<AuthOptions>>((identity, auth) =>
            {
                identity.User.RequireUniqueEmail = true;
                identity.Password.RequiredLength = auth.Value.PasswordMinimumLength;
                identity.Password.RequireDigit = false;
                identity.Password.RequireLowercase = false;
                identity.Password.RequireUppercase = false;
                identity.Password.RequireNonAlphanumeric = false;
                identity.Password.RequiredUniqueChars = PasswordRequiredUniqueCharacters;
                identity.Lockout.AllowedForNewUsers = true;
                identity.Lockout.MaxFailedAccessAttempts = auth.Value.MaxFailedAccessAttempts;
                identity.Lockout.DefaultLockoutTimeSpan = auth.Value.LockoutDuration;
                identity.Tokens.EmailConfirmationTokenProvider = AuthConstants.EmailConfirmationTokenProvider;
                identity.Tokens.PasswordResetTokenProvider = AuthConstants.PasswordResetTokenProvider;
            });

        services.AddIdentityCore<User>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddTokenProvider<EmailConfirmationTokenProvider>(AuthConstants.EmailConfirmationTokenProvider)
            .AddTokenProvider<PasswordResetTokenProvider>(AuthConstants.PasswordResetTokenProvider);

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>>((jwt, auth) => JwtBearerSettings.Apply(jwt, auth.Value));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<InvitationOptions>()
            .Bind(configuration.GetSection(InvitationOptions.SectionName))
            .Validate(options => options.Lifetime >= InvitationDefaults.MinimumLifetime && options.Lifetime <= InvitationDefaults.MaximumLifetime, "Invitation lifetime is out of range.")
            .ValidateOnStart();

        services.AddHybridCache();
        // Hosted services start in registration order and each StartAsync is awaited before
        // the next one starts. The migrations must be registered before the outbox dispatcher,
        // which reads the identity schema as soon as it starts.
        services.AddHostedService<IdentityMigrationsHostedService>();
        services.AddOutbox<IdentityDbContext>(configuration);
        services.AddIntegrationEventConsumer<AuditRecorded, IdentityDbContext, AuditRecordedConsumer>();

        services.AddValidatorsFromAssemblyContaining<RegisterAccountCommandValidator>();
        services.AddScoped<RefreshTokenService>();
        services.AddSingleton<AccessTokenIssuer>();
        services.AddSingleton<DummyPasswordHash>();
        services.AddSingleton<AuthEmailLinks>();
        services.AddScoped<AuthEmailScheduler>();
        services.AddScoped<SendConfirmationEmailJob>();
        services.AddScoped<SendPasswordResetEmailJob>();
        services.AddScoped<SendInvitationEmailJob>();
        services.AddScoped<RolePermissionCache>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IUserScopeReader, UserScopeReader>();
        services.AddScoped<IDataScope, DataScopeService>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IOrganizationDirectory, OrganizationDirectory>();
        services.AddScoped<IIdentityDemoSeeder, IdentityDemoSeeder>();

        return services;
    }
}
