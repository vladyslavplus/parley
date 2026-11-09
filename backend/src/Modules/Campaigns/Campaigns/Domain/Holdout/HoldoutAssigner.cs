using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Shared;

namespace Parley.Campaigns.Domain.Holdout;

/// <summary>
/// Deterministic holdout assignment (domain model 7.1): the bucket of a contact is the big-endian first eight bytes of
/// SHA-256("{seed}:{contact id}") modulo 10000, and the holdout arm takes the buckets below the share. Same seed and
/// contact give the same arm in C# and in the Python reference; the shared vectors in the unit tests keep them in step.
/// </summary>
public static class HoldoutAssigner
{
    public const int BucketCount = BasisPoints.Scale;

    private const int DigestPrefixBytes = 8;

    /// <summary>The bucket, 0 to 9999, of a contact for a holdout seed.</summary>
    public static int Bucket(string holdoutSeed, Guid contactId)
    {
        ArgumentException.ThrowIfNullOrEmpty(holdoutSeed);

        var payload = Encoding.UTF8.GetBytes($"{holdoutSeed}:{contactId:D}");
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(payload, digest);
        return (int)(BinaryPrimitives.ReadUInt64BigEndian(digest[..DigestPrefixBytes]) % BucketCount);
    }

    /// <summary>The arm of a contact. <paramref name="shareBps"/> is the holdout share in basis points.</summary>
    public static string Assign(string holdoutSeed, Guid contactId, int shareBps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(shareBps);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(shareBps, BucketCount);

        return Bucket(holdoutSeed, contactId) < shareBps ? HoldoutArms.Holdout : HoldoutArms.Treatment;
    }
}
