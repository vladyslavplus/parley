namespace Parley.Campaigns.Domain.Lift;

/// <summary>
/// Window state and finality of a stored campaign result. A result is final only when the attribution job computed it at or
/// after the window end: a figure computed earlier is provisional, even when the clock has since passed the window end.
/// That stops a final claim being made from partial outcomes while the daily sweep is late.
/// </summary>
public static class ResultStatus
{
    /// <summary>The campaign has no send time yet, so no window exists.</summary>
    public const string NotStarted = "not_started";

    /// <summary>The attribution window is still open.</summary>
    public const string Open = "open";

    /// <summary>The window has closed but no attribution run has computed the final figures yet.</summary>
    public const string AwaitingFinal = "awaiting_final";

    /// <summary>The window has closed and the stored figures were computed after it. These figures are final.</summary>
    public const string Closed = "closed";

    /// <summary>True when the stored result was computed at or after the window end.</summary>
    public static bool IsFinal(DateTime? windowEndsAt, DateTime? computedAt) =>
        windowEndsAt is { } end && computedAt is { } computed && computed >= end;

    /// <summary>The window state as of <paramref name="now"/>, with the stored figures' computation instant.</summary>
    public static string WindowState(DateTime? windowEndsAt, DateTime? computedAt, DateTime now)
    {
        if (windowEndsAt is not { } end)
        {
            return NotStarted;
        }

        if (now < end)
        {
            return Open;
        }

        return IsFinal(windowEndsAt, computedAt) ? Closed : AwaitingFinal;
    }
}
