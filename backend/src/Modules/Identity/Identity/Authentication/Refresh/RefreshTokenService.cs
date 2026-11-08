using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Identity.Authentication.Tokens;
using Parley.Identity.Authorization.Permissions;
using Parley.Identity.Contracts.Errors;
using Parley.Identity.Domain.Sessions;
using Parley.Identity.Options.Auth;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Authentication.Refresh;

/// <summary>
/// Issues, rotates and revokes refresh tokens.
/// </summary>
/// <remarks>
/// <para>
/// Refresh values are opaque random strings. Only their SHA-256 hash is stored, so a
/// database leak does not yield usable tokens. The lookup is an indexed equality on
/// that hash, which reveals nothing about the value itself.
/// </para>
/// <para>
/// Rotation is one transaction: insert the successor, then flip the presented row
/// from active to revoked with a conditional update. Two requests racing on the same
/// token cannot both win; the loser sees zero affected rows and takes the grace path.
/// </para>
/// <para>
/// Reuse rules. A rotated token presented within <see cref="AuthOptions.RefreshReuseGraceSeconds"/>
/// of its rotation returns a fresh token in the same family (two browser tabs refreshing
/// at once is normal). A rotated token presented after the grace, or any revoked token
/// presented again, revokes the whole family, so the successor stops working too.
/// </para>
/// <para>
/// External I/O never runs here. Time comes from <see cref="TimeProvider"/>.
/// </para>
/// </remarks>
public sealed class RefreshTokenService(
    IdentityDbContext dbContext,
    AccessTokenIssuer accessTokenIssuer,
    IOptions<AuthOptions> authOptions,
    TimeProvider timeProvider)
{
    private readonly AuthOptions _options = authOptions.Value;

    /// <summary>Starts a new session family for a user who just signed in.</summary>
    public async Task<AuthTokens> IssueNewFamilyAsync(Guid userId, Guid organizationId, bool isPlatformOperator, CancellationToken cancellationToken)
    {
        var familyId = Guid.CreateVersion7();
        var (refreshValue, refreshExpiresAt, _) = InsertRefreshToken(userId, familyId, organizationId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatePair(userId, organizationId, isPlatformOperator, refreshValue, refreshExpiresAt);
    }

    /// <summary>
    /// Exchanges an active refresh value for a new pair. Returns
    /// <see cref="IdentityErrorCodes.AuthRefreshTokenInvalid"/> for unknown, expired or
    /// revoked values and <see cref="IdentityErrorCodes.AuthRefreshTokenReused"/> when a
    /// rotated value is presented after the grace window.
    /// </summary>
    public async Task<Result<AuthTokens>> RotateAsync(string presentedValue, CancellationToken cancellationToken)
    {
        var hash = HashValue(presentedValue);
        var nowUtc = timeProvider.GetUtcNow();

        var rotated = await TryRotateAsync(hash, nowUtc, cancellationToken);
        if (rotated is not null)
        {
            return rotated;
        }

        var current = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == hash)
            .Select(token => new TokenState(
                token.UserId,
                token.FamilyId,
                token.OrganizationId,
                token.ExpiresAt,
                token.RevokedAt,
                token.ReplacedById))
            .FirstOrDefaultAsync(cancellationToken);

        if (current is null)
        {
            return Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthRefreshTokenInvalid));
        }

        if (current.RevokedAt is null)
        {
            // Active but not rotated here means it expired.
            return Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthRefreshTokenInvalid));
        }

        if (!await ActiveMembershipQueries.IsActiveAsync(dbContext, current.UserId, current.OrganizationId, cancellationToken))
        {
            return Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthRefreshTokenInvalid));
        }

        var graceEnd = current.RevokedAt.Value.AddSeconds(_options.RefreshReuseGraceSeconds);
        if (current.ReplacedById is not null && nowUtc.UtcDateTime <= graceEnd)
        {
            // Rotation always sets ReplacedById; logout, password reset and family revocation never do.
            // A terminal revocation in the family means the session was ended, so the grace cannot revive it.
            var familyTerminated = await dbContext.RefreshTokens
                .AsNoTracking()
                .AnyAsync(token => token.FamilyId == current.FamilyId
                    && token.RevokedAt != null
                    && token.ReplacedById == null, cancellationToken);
            if (!familyTerminated)
            {
                var sameFamily = await IssueInFamilyAsync(current, cancellationToken);
                return Result<AuthTokens>.Success(sameFamily);
            }

            return Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthRefreshTokenInvalid));
        }

        await RevokeFamilyAsync(current.FamilyId, nowUtc, cancellationToken);
        var code = current.ReplacedById is not null
            ? IdentityErrorCodes.AuthRefreshTokenReused
            : IdentityErrorCodes.AuthRefreshTokenInvalid;
        return Result<AuthTokens>.Failure(Error.Unauthorized(code));
    }

    /// <summary>
    /// Ends the session that owns the presented value. Unknown values are accepted
    /// silently, so logout never reveals whether a value existed.
    /// </summary>
    public async Task RevokeSessionAsync(string presentedValue, CancellationToken cancellationToken)
    {
        var hash = HashValue(presentedValue);
        var familyId = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == hash)
            .Select(token => (Guid?)token.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (familyId is not null)
        {
            await RevokeFamilyAsync(familyId.Value, timeProvider.GetUtcNow(), cancellationToken);
        }
    }

    /// <summary>
    /// Ends the session that owns the presented value, but only when the value belongs to
    /// <paramref name="userId"/> and is still active. Returns false and changes nothing otherwise.
    /// </summary>
    public async Task<bool> EndOwnedSessionAsync(string presentedValue, Guid userId, CancellationToken cancellationToken)
    {
        var hash = HashValue(presentedValue);
        var nowUtc = timeProvider.GetUtcNow();
        var familyId = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == hash
                && token.UserId == userId
                && token.RevokedAt == null
                && token.ExpiresAt > nowUtc.UtcDateTime)
            .Select(token => (Guid?)token.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (familyId is null)
        {
            return false;
        }

        await RevokeFamilyAsync(familyId.Value, nowUtc, cancellationToken);
        return true;
    }

    /// <summary>Revokes every active refresh token of a user (after a password reset).</summary>
    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, nowUtc), cancellationToken);
    }

    /// <summary>
    /// Rotates the presented value in one transaction. Returns null when the value is
    /// not rotatable here (already rotated by a racing request, revoked or expired), so the
    /// caller takes the grace or revocation path; a failure when the membership of the
    /// session is no longer active.
    /// </summary>
    private async Task<Result<AuthTokens>?> TryRotateAsync(byte[] presentedHash, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            var current = await dbContext.RefreshTokens
                .AsNoTracking()
                .Where(token => token.TokenHash == presentedHash)
                .Select(token => new TokenState(
                    token.UserId,
                    token.FamilyId,
                    token.OrganizationId,
                    token.ExpiresAt,
                    token.RevokedAt,
                    token.ReplacedById))
                .FirstOrDefaultAsync(ct);

            if (current is null
                || current.RevokedAt is not null
                || current.ExpiresAt <= nowUtc.UtcDateTime)
            {
                await transaction.RollbackAsync(ct);
                return null;
            }

            if (!await ActiveMembershipQueries.IsActiveAsync(dbContext, current.UserId, current.OrganizationId, ct))
            {
                await transaction.RollbackAsync(ct);
                return Result<AuthTokens>.Failure(Error.Unauthorized(IdentityErrorCodes.AuthRefreshTokenInvalid));
            }

            var (refreshValue, refreshExpiresAt, successorId) = InsertRefreshToken(
                current.UserId,
                current.FamilyId,
                current.OrganizationId);
            await dbContext.SaveChangesAsync(ct);
            var isPlatformOperator = await ReadPlatformOperatorAsync(current.UserId, ct);

            var affected = await dbContext.RefreshTokens
                .Where(token => token.TokenHash == presentedHash && token.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.RevokedAt, nowUtc.UtcDateTime)
                    .SetProperty(token => token.ReplacedById, (Guid?)successorId), ct);

            if (affected == 0)
            {
                // Another request rotated this token first; the caller takes the grace path.
                await transaction.RollbackAsync(ct);
                return null;
            }

            await transaction.CommitAsync(ct);
            return Result<AuthTokens>.Success(CreatePair(current.UserId, current.OrganizationId, isPlatformOperator, refreshValue, refreshExpiresAt));
        }, cancellationToken);
    }

    private async Task<AuthTokens> IssueInFamilyAsync(TokenState source, CancellationToken cancellationToken)
    {
        var (refreshValue, refreshExpiresAt, _) = InsertRefreshToken(
            source.UserId,
            source.FamilyId,
            source.OrganizationId);
        await dbContext.SaveChangesAsync(cancellationToken);
        var isPlatformOperator = await ReadPlatformOperatorAsync(source.UserId, cancellationToken);
        return CreatePair(source.UserId, source.OrganizationId, isPlatformOperator, refreshValue, refreshExpiresAt);
    }

    private async Task<bool> ReadPlatformOperatorAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.IsPlatformOperator)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, nowUtc.UtcDateTime), cancellationToken);
    }

    /// <summary>Adds a new tracked row and returns its opaque value; the caller saves it.</summary>
    private (string Value, DateTimeOffset ExpiresAt, Guid Id) InsertRefreshToken(
        Guid userId,
        Guid familyId,
        Guid organizationId)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var value = CreateOpaqueValue();
        var expiresAt = nowUtc.Add(_options.RefreshTokenLifetime);
        var row = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = HashValue(value),
            OrganizationId = organizationId,
            ExpiresAt = expiresAt.UtcDateTime,
            CreatedAt = nowUtc.UtcDateTime,
        };
        dbContext.RefreshTokens.Add(row);
        return (value, expiresAt, row.Id);
    }

    private AuthTokens CreatePair(
        Guid userId,
        Guid organizationId,
        bool isPlatformOperator,
        string refreshValue,
        DateTimeOffset refreshExpiresAt)
    {
        var (accessToken, accessExpiresAt) = accessTokenIssuer.Issue(userId, organizationId, isPlatformOperator);
        return new AuthTokens(accessToken, accessExpiresAt, refreshValue, refreshExpiresAt);
    }

    private static string CreateOpaqueValue() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(AuthConstants.RefreshTokenByteLength));

    private static byte[] HashValue(string value) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private sealed record TokenState(
        Guid UserId,
        Guid FamilyId,
        Guid OrganizationId,
        DateTime ExpiresAt,
        DateTime? RevokedAt,
        Guid? ReplacedById);
}
