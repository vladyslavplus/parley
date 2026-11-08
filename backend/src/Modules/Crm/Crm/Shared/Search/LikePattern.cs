using System.Text;

namespace Parley.Crm.Shared.Search;

/// <summary>
/// Builds a case-insensitive "contains" pattern for <c>ILIKE</c> with the wildcard
/// characters of the user input escaped, so a term matches literally.
/// </summary>
public static class LikePattern
{
    private const char Escape = '\\';

    private const char Wildcard = '%';

    private const char SingleCharacter = '_';

    private const string Any = "%";

    public static string Contains(string term)
    {
        var builder = new StringBuilder(term.Length + 2).Append(Any);
        foreach (var character in term.Trim().ToLowerInvariant())
        {
            if (character is Escape or Wildcard or SingleCharacter)
            {
                builder.Append(Escape);
            }

            builder.Append(character);
        }

        return builder.Append(Any).ToString();
    }
}
