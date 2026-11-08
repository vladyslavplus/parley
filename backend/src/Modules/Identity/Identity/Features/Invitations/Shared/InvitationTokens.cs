using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Parley.Identity.Features.Invitations.Shared;

/// <summary>
/// Invitation token values. The token is random, sent only in the email, and stored as a
/// SHA-256 hash; comparisons use a fixed-time check.
/// </summary>
public static class InvitationTokens
{
    /// <summary>Random bytes behind each invitation token (256 bits).</summary>
    private const int ByteLength = 32;

    public static string Create() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(ByteLength));

    /// <summary>
    /// A placeholder hash for the token column at creation; the delivery job replaces it with
    /// the hash of the token it sends. The source value is random, never stored or sent, so no
    /// presented token can match it: a match needs a SHA-256 preimage of that value. Accept
    /// therefore refuses an invitation whose delivery has not run yet.
    /// </summary>
    public static byte[] CreateUnknownHash() => Hash(Create());

    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    public static bool Matches(byte[] storedHash, byte[] candidateHash) =>
        CryptographicOperations.FixedTimeEquals(storedHash, candidateHash);
}
