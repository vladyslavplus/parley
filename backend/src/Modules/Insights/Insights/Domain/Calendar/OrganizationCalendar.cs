namespace Parley.Insights.Domain.Calendar;

/// <summary>The calendar day of an organization: the instant read in the organization's time zone.</summary>
public static class OrganizationCalendar
{
    /// <summary>The local calendar date of <paramref name="instant"/> in the IANA zone <paramref name="timeZoneId"/>.</summary>
    public static DateOnly LocalDate(DateTimeOffset instant, string timeZoneId)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);
    }

    /// <summary>True when the IANA zone name is known to the host.</summary>
    public static bool IsKnownZone(string timeZoneId)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
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
