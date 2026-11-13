using AwesomeAssertions;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Seeding.DemoSeed;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Domain.Rfm;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.UnitTests.Crm.Seeding;

/// <summary>
/// The generated demo data is deterministic for a fixed seed and reference instant, has no future order, and covers every
/// RFM segment when the real quintile rules run over its orders.
/// </summary>
public sealed class DemoGeneratorTests
{
    private static readonly DateTimeOffset Reference = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private static readonly Guid[] Owners = [Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7()];

    private static readonly CrmDemoImportRequest Request = new(
        Guid.CreateVersion7(),
        CrmDemoContract.DemoCurrency,
        CultureCodes.Ukrainian,
        Owners,
        20261112,
        Reference,
        new CrmDemoProfile());

    [Fact]
    public void The_same_seed_and_reference_give_the_same_contacts_and_orders()
    {
        var firstContacts = DemoGenerator.Contacts(Request);
        var firstOrders = DemoGenerator.Orders(firstContacts, Request);
        var secondContacts = DemoGenerator.Contacts(Request);
        var secondOrders = DemoGenerator.Orders(secondContacts, Request);

        secondContacts.Should().Equal(firstContacts);
        secondOrders.Should().Equal(firstOrders);
    }

    [Fact]
    public void No_order_lies_after_the_reference_instant()
    {
        var contacts = DemoGenerator.Contacts(Request);
        var orders = DemoGenerator.Orders(contacts, Request);

        orders.Should().NotBeEmpty();
        orders.Should().OnlyContain(order => order.OrderedAt < Reference.UtcDateTime);
        orders.Max(order => order.OrderedAt).Date.Should().Be(Reference.UtcDateTime.Date.AddDays(-1));
    }

    [Fact]
    public void Every_rfm_segment_is_reachable_from_the_generated_orders()
    {
        var contacts = DemoGenerator.Contacts(Request);
        var orders = DemoGenerator.Orders(contacts, Request);
        var byContact = orders.GroupBy(order => order.CustomerRef).ToDictionary(group => group.Key, group => group.ToList());
        var inputs = new List<RfmInput>(contacts.Count);
        foreach (var contact in contacts)
        {
            if (!byContact.TryGetValue(contact.CustomerRef, out var own) || own.Count == 0)
            {
                continue;
            }

            var last = own.Max(order => order.OrderedAt);
            var windowStart = Reference.UtcDateTime.AddDays(-365);
            var inWindow = own.Where(order => order.OrderedAt > windowStart).ToList();
            inputs.Add(new RfmInput(
                contact.Id,
                contact.OwnerUserId,
                RfmRecency.DaysBetween(Reference, last),
                inWindow.Count(order => !order.IsCancellation),
                inWindow.Sum(order => order.TotalAmount)));
        }

        var tiers = RfmTierCalculator.Compute(inputs);
        var segments = inputs.Select(input => RfmSegmentRules.Classify(tiers[input.ContactId])).Distinct().ToHashSet(StringComparer.Ordinal);
        foreach (var expected in RfmSegmentNames.All.Where(segment => segment != RfmSegmentNames.NoOrders))
        {
            segments.Should().Contain(expected);
        }
        contacts.Should().Contain(contact => !byContact.ContainsKey(contact.CustomerRef), "the no-orders segment is populated too");
    }
}
