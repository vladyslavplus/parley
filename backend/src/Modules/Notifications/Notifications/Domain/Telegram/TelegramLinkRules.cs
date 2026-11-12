using System.Security.Cryptography;
using System.Text;

namespace Parley.Notifications.Domain.Telegram;

/// <summary>Rules of the link codes: shape, hashing, and whether a code can still be redeemed.</summary>
public static class TelegramLinkRules
{
    /// <summary>Characters in a code. 32 characters from a 31-character alphabet carry about 158 random bits.</summary>
    public const int CodeLength = 32;

    /// <summary>Lowercase letters and digits without look-alikes. Telegram start payloads accept these characters.</summary>
    private const string CodeAlphabet = "abcdefghjkmnpqrstuvwxyz23456789";

    /// <summary>A fresh random code. The caller shows it once and stores only its hash.</summary>
    public static string NewCode()
    {
        var code = new char[CodeLength];
        for (var index = 0; index < code.Length; index++)
        {
            code[index] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        }

        return new string(code);
    }

    /// <summary>True when the text has the length and alphabet of a code. Anything else is refused before any lookup.</summary>
    public static bool HasValidShape(string? code)
    {
        if (code is not { Length: CodeLength })
        {
            return false;
        }

        foreach (var character in code)
        {
            if (CodeAlphabet.IndexOf(character, StringComparison.Ordinal) < 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>SHA-256 of the code. The codes are random and long, so a plain hash is enough and nothing needs a secret key.</summary>
    public static byte[] HashCode(string code) => SHA256.HashData(Encoding.UTF8.GetBytes(code));

    /// <summary>A code is redeemable once and only before it expires.</summary>
    public static bool IsRedeemable(DateTime expiresAt, DateTime? usedAt, DateTime now) => usedAt is null && now < expiresAt;

    /// <summary>The link that starts the bot with the code in the start payload.</summary>
    public static string DeepLink(string botUsername, string code) => $"https://t.me/{botUsername}?start={code}";
}
