using System.Globalization;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Shared.Pipelines;
using Parley.SharedKernel.Localization;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Generates the demo workspace from a fixed seed and a reference instant. Every choice is a deterministic bucket of the
/// contact number, so the same seed gives the same contacts, orders and workspace records; dates are relative to the
/// reference instant, never hard-coded. Change the archetype mix in <see cref="DemoArchetypes"/> when the RFM rules change,
/// and the workspace shares here when the domain adds a required field (a new stage, consent flag, or contact field).
/// </summary>
public static class DemoGenerator
{
    private const int HoursInDay = 24;

    private const int OrderStartHour = 9;

    private const int OrderHourSpread = 10;

    private static readonly string[] DemoTagNames =
    [
        "VIP", "Wholesale", "Seasonal", "Newsletter", "Priority",
    ];

    public static IReadOnlyList<DemoContactSeed> Contacts(CrmDemoImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SalesOwnerUserIds.Count == 0)
        {
            throw new ArgumentException("The demo workspace needs at least one sales owner.", nameof(request));
        }

        var profile = request.Profile;
        var now = request.ReferenceInstant.UtcDateTime;
        var contacts = new List<DemoContactSeed>(profile.Contacts);
        for (var index = 0; index < profile.Contacts; index++)
        {
            contacts.Add(CreateContact(index, request, now));
        }

        return contacts;
    }

    public static IReadOnlyList<DemoOrderSeed> Orders(IReadOnlyList<DemoContactSeed> contacts, CrmDemoImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        ArgumentNullException.ThrowIfNull(request);
        var profile = request.Profile;
        var end = EndOfYesterday(request.ReferenceInstant);
        var start = end.AddMonths(-profile.OrderMonths);
        var orders = new List<DemoOrderSeed>(contacts.Count * 10);
        foreach (var contact in contacts)
        {
            var archetype = DemoArchetypes.For(contact.Index);
            if (archetype.Archetype == DemoArchetype.NoOrders)
            {
                continue;
            }

            var random = new Random(DemoIdentifiers.Seed(request.RandomSeed, contact.Index, "orders"));
            var count = random.Next(archetype.MinimumOrders, archetype.MaximumOrders + 1);
            var lastDays = random.Next(archetype.MinimumLastOrderDays, archetype.MaximumLastOrderDays + 1);
            var last = end.AddDays(-lastDays).AddHours(-random.Next(0, OrderHourSpread));
            if (last >= end)
            {
                last = end.AddHours(-1);
            }

            var gap = random.Next(archetype.MinimumGapDays, archetype.MaximumGapDays + 1);
            var first = last.AddDays(-(count - 1) * gap);
            if (first < start)
            {
                gap = Math.Max(1, (int)((last - start).TotalDays / Math.Max(1, count - 1)));
                first = last.AddDays(-(count - 1) * gap);
            }

            for (var orderIndex = 0; orderIndex < count; orderIndex++)
            {
                var orderedAt = first.AddDays(orderIndex * gap).AddHours(-random.Next(0, OrderHourSpread));
                if (orderedAt >= end)
                {
                    orderedAt = end.AddHours(-1);
                }

                if (orderedAt < start)
                {
                    orderedAt = start;
                }

                var baseAmount = decimal.Round(
                    archetype.MinimumAmount + (decimal)random.NextDouble() * (archetype.MaximumAmount - archetype.MinimumAmount),
                    2);
                var cancellation = count >= 6
                    && orderIndex > 0
                    && orderIndex < count - 1
                    && DemoIdentifiers.Bucket(
                        $"{contact.CustomerRef}:{orderIndex}",
                        "cancellation",
                        100) < profile.CancelledOrderPercent;
                var amount = cancellation ? -decimal.Round(baseAmount / 2m, 2) : baseAmount;
                orders.Add(new DemoOrderSeed(
                    DemoIdentifiers.Derive(DemoSeedConstants.OrderKind, $"{contact.CustomerRef}:{orderIndex}"),
                    $"DEMO-{contact.Index:D6}-{orderIndex:D3}",
                    contact.CustomerRef,
                    DateTime.SpecifyKind(orderedAt, DateTimeKind.Utc),
                    amount,
                    random.Next(1, 6),
                    cancellation));
            }
        }

        return orders;
    }

    public static IReadOnlyList<string> TagNames => DemoTagNames;

    private static DemoContactSeed CreateContact(int index, CrmDemoImportRequest request, DateTime now)
    {
        var profile = request.Profile;
        var locale = request.Locale;
        var archetype = DemoArchetypes.For(index);
        var customerRef = index.ToString("D6", CultureInfo.InvariantCulture);
        var firstName = DemoNamePool.First(index);
        var lastName = DemoNamePool.Last(index);
        var place = DemoNamePool.Place(index);
        var hasCompany = DemoIdentifiers.Bucket(customerRef, "company", 100) < profile.CompanySharePercent;
        var companyIndex = index / 3;
        var companyId = hasCompany ? DemoIdentifiers.Derive(DemoSeedConstants.CompanyKind, companyIndex.ToString(CultureInfo.InvariantCulture)) : (Guid?)null;
        var companyName = hasCompany ? DemoNamePool.CompanyName(companyIndex) : null;
        var owner = request.SalesOwnerUserIds[index % request.SalesOwnerUserIds.Count];

        int? dealStageIndex = null;
        string? dealTitle = null;
        string? lostReason = null;
        DateTime? expectedCloseOn = null;
        if (DemoIdentifiers.Bucket(customerRef, "deal", 100) < profile.DealSharePercent)
        {
            var stage = DemoWorkspaceRules.StageByBucket[DemoIdentifiers.Bucket(customerRef, "stage", DemoWorkspaceRules.StageByBucket.Count)];
            dealStageIndex = stage;
            var suffix = Text(Pick(DemoWorkspaceRules.DealSuffixKeys, customerRef, "deal-title"), locale);
            dealTitle = $"{companyName ?? $"{firstName} {lastName}"} - {suffix}";
            if (stage == DemoWorkspaceRules.LostStageIndex)
            {
                lostReason = Text(Pick(DemoWorkspaceRules.LostReasonKeys, customerRef, "lost-reason"), locale);
            }

            var stageKind = DefaultPipelineStages.All[stage].Kind;
            if (stageKind == PipelineStageKinds.Open)
            {
                var offset = DemoIdentifiers.Bucket(customerRef, "close", DemoWorkspaceRules.ExpectedCloseDaysBefore + DemoWorkspaceRules.ExpectedCloseDaysAfter)
                    - DemoWorkspaceRules.ExpectedCloseDaysBefore;
                expectedCloseOn = now.Date.AddDays(offset);
            }
        }

        string? taskTitle = null;
        DateTime? taskDueAt = null;
        var taskCompleted = false;
        if (DemoIdentifiers.Bucket(customerRef, "task", 100) < profile.TaskSharePercent)
        {
            taskTitle = Text(Pick(DemoWorkspaceRules.TaskTitleKeys, customerRef, "task-title"), locale);
            var offsetDays = DemoIdentifiers.Bucket(customerRef, "due", DemoWorkspaceRules.TaskDueSpreadDays)
                - DemoWorkspaceRules.TaskDueCenterDays;
            taskDueAt = DateTime.SpecifyKind(now.Date.AddDays(offsetDays).AddHours(OrderStartHour), DateTimeKind.Utc);
            taskCompleted = taskDueAt < now;
        }

        string? noteBody = null;
        if (DemoIdentifiers.Bucket(customerRef, "note", 100) < profile.NoteSharePercent)
        {
            noteBody = Text(Pick(DemoWorkspaceRules.NoteBodyKeys, customerRef, "note-body"), locale);
        }

        return new DemoContactSeed(
            index,
            archetype.Archetype,
            DemoIdentifiers.Derive(DemoSeedConstants.ContactKind, customerRef),
            customerRef,
            firstName,
            lastName,
            $"demo{customerRef}@{DemoSeedConstants.EmailDomain}",
            Phone(customerRef),
            DemoNamePool.JobTitle(index),
            place.Country,
            place.City,
            owner,
            companyId,
            companyName,
            dealStageIndex,
            dealTitle,
            lostReason,
            expectedCloseOn,
            taskTitle,
            taskDueAt,
            taskCompleted,
            noteBody);
    }

    private static DateTime EndOfYesterday(DateTimeOffset reference)
    {
        var yesterday = reference.UtcDateTime.Date.AddDays(-1);
        return new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 23, 0, 0, DateTimeKind.Utc);
    }

    private static string Phone(string customerRef)
    {
        var digits = DemoIdentifiers.Bucket(customerRef, "phone", 100_000_000);
        return $"+380 ({digits / 10_000_000 % 100:00}) {digits / 100_000 % 1000:000}-{digits % 100_000:00000}";
    }

    private static string Pick(IReadOnlyList<string> keys, string customerRef, string decision) =>
        keys[DemoIdentifiers.Bucket(customerRef, decision, keys.Count)];

    private static string Text(string key, string locale) => SharedTexts.Get(key, locale);
}
