using System.Text;

namespace Parley.SharedKernel.Paging;

/// <summary>
/// Opaque position of a keyset page: the sort value of the last row and its identifier.
/// The identifier breaks ties, so rows with equal sort values are returned exactly once.
/// Encoded as URL-safe Base64 so the value travels in a query string unchanged.
/// </summary>
/// <param name="SortKey">Serialized sort value of the last row of the previous page.</param>
/// <param name="Id">Identifier of the last row of the previous page.</param>
public sealed record KeysetCursor(string SortKey, Guid Id)
{
    private const char Separator = '\u001F';

    private const char Base64Padding = '=';

    private const char StandardPlus = '+';

    private const char StandardSlash = '/';

    private const char UrlSafePlus = '-';

    private const char UrlSafeSlash = '_';

    private const int Base64Quantum = 4;

    private const string IdFormat = "N";

    public string Encode()
    {
        var bytes = Encoding.UTF8.GetBytes(SortKey + Separator + Id.ToString(IdFormat));
        return Convert.ToBase64String(bytes)
            .TrimEnd(Base64Padding)
            .Replace(StandardPlus, UrlSafePlus)
            .Replace(StandardSlash, UrlSafeSlash);
    }

    /// <summary>Decodes a cursor produced by <see cref="Encode"/>. Returns false for any malformed value.</summary>
    public static bool TryDecode(string? value, out KeysetCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        try
        {
            var padded = value.Replace(UrlSafePlus, StandardPlus).Replace(UrlSafeSlash, StandardSlash);
            var remainder = padded.Length % Base64Quantum;
            if (remainder != 0)
            {
                padded += new string(Base64Padding, Base64Quantum - remainder);
            }

            var text = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var separatorIndex = text.LastIndexOf(Separator);
            if (separatorIndex < 0 || !Guid.TryParseExact(text[(separatorIndex + 1)..], IdFormat, out var id))
            {
                return false;
            }

            cursor = new KeysetCursor(text[..separatorIndex], id);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
