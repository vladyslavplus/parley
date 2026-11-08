namespace Parley.Identity.Shared.Validation;

/// <summary>
/// Checks IANA time zone identifiers against the time zone database of the host.
/// </summary>
public static class IanaTimeZones
{
    public static bool IsKnown(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(identifier);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
