using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Options;

namespace Parley.Campaigns.Sending.Unsubscribe;

/// <summary>
/// Issues and checks unsubscribe tokens. A token is <c>base64url(payload) "." base64url(HMAC-SHA256(payload))</c>, where
/// the payload names a purpose, the organization, the campaign, the contact and the expiry. The key is the configured
/// secret. Changing any part of a token breaks its signature, so a token cannot be moved to another organization,
/// campaign or contact.
/// </summary>
public sealed class UnsubscribeTokenCodec(IOptions<CampaignsSendOptions> options, TimeProvider timeProvider)
{
    private const char TokenSeparator = '.';

    private const char FieldSeparator = '|';

    private const int PayloadFieldCount = 5;

    private const string Purpose = "campaigns-unsubscribe";

    private const string GuidFormat = "N";

    private static readonly int SignatureLength = HMACSHA256.HashSizeInBytes;

    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.Value.UnsubscribeTokenKey);

    private readonly TimeSpan _lifetime = options.Value.UnsubscribeTokenLifetime;

    /// <summary>Issues a token for one recipient of one campaign. Valid for the configured lifetime from now.</summary>
    public string Issue(Guid organizationId, Guid campaignId, Guid contactId)
    {
        var expiresAt = timeProvider.GetUtcNow().Add(_lifetime).ToUnixTimeSeconds();
        var payload = Encoding.UTF8.GetBytes(string.Join(
            FieldSeparator,
            Purpose,
            organizationId.ToString(GuidFormat),
            campaignId.ToString(GuidFormat),
            contactId.ToString(GuidFormat),
            expiresAt.ToString(CultureInfo.InvariantCulture)));
        var signature = HMACSHA256.HashData(_key, payload);
        return Base64Url.EncodeToString(payload) + TokenSeparator + Base64Url.EncodeToString(signature);
    }

    /// <summary>Checks the signature, the purpose and the lifetime of a token. Never throws for a malformed token.</summary>
    public UnsubscribeTokenVerification Verify(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return UnsubscribeTokenVerification.Invalid;
        }

        var parts = token.Split(TokenSeparator);
        if (parts.Length != 2
            || !TryDecode(parts[0], out var payload)
            || !TryDecode(parts[1], out var signature)
            || signature.Length != SignatureLength)
        {
            return UnsubscribeTokenVerification.Invalid;
        }

        var expected = HMACSHA256.HashData(_key, payload);
        if (!CryptographicOperations.FixedTimeEquals(expected, signature))
        {
            return UnsubscribeTokenVerification.Invalid;
        }

        var claims = ParsePayload(payload);
        if (claims is null)
        {
            return UnsubscribeTokenVerification.Invalid;
        }

        return timeProvider.GetUtcNow() >= claims.ExpiresAt
            ? UnsubscribeTokenVerification.Expired
            : new UnsubscribeTokenVerification(UnsubscribeTokenCheck.Valid, claims);
    }

    private static bool TryDecode(string text, out byte[] bytes)
    {
        try
        {
            bytes = Base64Url.DecodeFromChars(text);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static UnsubscribeTokenClaims? ParsePayload(byte[] payload)
    {
        var fields = Encoding.UTF8.GetString(payload).Split(FieldSeparator);
        if (fields.Length != PayloadFieldCount
            || fields[0] != Purpose
            || !Guid.TryParseExact(fields[1], GuidFormat, out var organizationId)
            || !Guid.TryParseExact(fields[2], GuidFormat, out var campaignId)
            || !Guid.TryParseExact(fields[3], GuidFormat, out var contactId)
            || !long.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAtSeconds))
        {
            return null;
        }

        return new UnsubscribeTokenClaims(
            organizationId,
            campaignId,
            contactId,
            DateTimeOffset.FromUnixTimeSeconds(expiresAtSeconds));
    }
}
