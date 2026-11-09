using System.Security.Cryptography;

namespace Parley.Identity.Seeding.DemoSeed;

/// <summary>Generates demo passwords from a cryptographic random source. Letters and digits only, so they paste anywhere.</summary>
public static class DemoPasswordGenerator
{
    private const string Alphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate(int length)
    {
        var characters = new char[length];
        for (var index = 0; index < characters.Length; index++)
        {
            characters[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(characters);
    }
}
