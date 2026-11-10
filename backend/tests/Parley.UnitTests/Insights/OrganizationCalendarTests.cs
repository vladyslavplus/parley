using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Parley.Insights.Domain.Calendar;
using Xunit;

namespace Parley.UnitTests.Insights;

/// <summary>The organization day flips at its own local midnight, not at the UTC one.</summary>
public sealed class OrganizationCalendarTests
{
    private const string KyivZone = "Europe/Kyiv";

    [Fact]
    public void LocalDate_FlipsAtKyivMidnight_NotAtUtcMidnight()
    {
        // November is UTC+2 in Kyiv: local midnight is 22:00 UTC of the day before.
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 9, 21, 59, 59, TimeSpan.Zero));

        OrganizationCalendar.LocalDate(clock.GetUtcNow(), KyivZone).Should().Be(new DateOnly(2026, 11, 9));

        clock.Advance(TimeSpan.FromSeconds(1));
        OrganizationCalendar.LocalDate(clock.GetUtcNow(), KyivZone).Should().Be(new DateOnly(2026, 11, 10));
    }

    [Fact]
    public void LocalDate_ForUtc_IsTheUtcDate()
    {
        var instant = new DateTimeOffset(2026, 11, 9, 23, 30, 0, TimeSpan.Zero);

        OrganizationCalendar.LocalDate(instant, "UTC").Should().Be(new DateOnly(2026, 11, 9));
    }

    [Fact]
    public void IsKnownZone_RejectsAnUnknownName()
    {
        OrganizationCalendar.IsKnownZone(KyivZone).Should().BeTrue();
        OrganizationCalendar.IsKnownZone("Mars/Olympus").Should().BeFalse();
    }
}
