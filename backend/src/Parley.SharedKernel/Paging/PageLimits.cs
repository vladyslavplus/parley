namespace Parley.SharedKernel.Paging;

/// <summary>
/// Page size rules for list endpoints. Every list is keyset-paged: the caller passes the opaque cursor
/// of the previous page. Requested sizes are clamped to the allowed range.
/// </summary>
public static class PageLimits
{
    /// <summary>Page size when the request gives none.</summary>
    public const int DefaultSize = 20;

    /// <summary>Largest page one request returns.</summary>
    public const int MaximumSize = 100;

    /// <summary>Returns the requested size clamped to the allowed range, or the default when none is given.</summary>
    public static int Resolve(int? requested) => requested switch
    {
        null => DefaultSize,
        < 1 => DefaultSize,
        > MaximumSize => MaximumSize,
        _ => requested.Value,
    };
}
