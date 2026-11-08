namespace Parley.SharedKernel.Paging;

/// <summary>
/// Page size rules for list endpoints. Every list is keyset-paged; the caller
/// passes the last identifier of the previous page as the cursor.
/// </summary>
public static class PageLimits
{
    private const int DefaultSize = 50;

    private const int MaximumSize = 200;

    /// <summary>Returns the requested size clamped to the allowed range, or the default when none is given.</summary>
    public static int Resolve(int? requested) => requested switch
    {
        null => DefaultSize,
        < 1 => DefaultSize,
        > MaximumSize => MaximumSize,
        _ => requested.Value,
    };
}
