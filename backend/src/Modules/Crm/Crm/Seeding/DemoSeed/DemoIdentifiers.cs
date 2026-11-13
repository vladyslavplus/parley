using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Stable identifiers and generation choices of the demo seed. Every value depends only on the source key and the
/// name of the decision, so the same export always yields the same rows.
/// </summary>
public static class DemoIdentifiers
{
    /// <summary>Identifier of a generated row: MD5 of the kind and key, rendered as a GUID.</summary>
    public static Guid Derive(string kind, string key)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(kind + ":" + key));
        return Guid.ParseExact(Convert.ToHexStringLower(hash), "N");
    }

    /// <summary>A number in [0, modulo) for one decision about one key.</summary>
    public static int Bucket(string key, string decision, int modulo)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(decision + ":" + key));
        return (int)(BinaryPrimitives.ReadUInt32BigEndian(hash) & 0x7FFFFFFF) % modulo;
    }

    /// <summary>A stable non-negative random seed for one generated row and purpose.</summary>
    public static int Seed(long seed, int index, string purpose)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"{seed}:{index}:{purpose}"));
        return BinaryPrimitives.ReadInt32LittleEndian(hash) & 0x7FFFFFFF;
    }
}
