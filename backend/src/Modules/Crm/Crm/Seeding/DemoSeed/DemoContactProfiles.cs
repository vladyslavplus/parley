using System.Globalization;
using Bogus;
using Parley.SharedKernel.Localization;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Builds the generated identity and workspace records of a demo contact. The generator is seeded with the numeric
/// customer id, and every choice is a hash of that id, so the same source id always yields the same record. Texts are
/// resolved in the given locale.
/// </summary>
public static class DemoContactProfiles
{
    public static DemoContactProfile Create(
        string customerRef,
        IReadOnlyList<Guid> salesOwnerUserIds,
        DateTimeOffset now,
        string locale)
    {
        ArgumentNullException.ThrowIfNull(salesOwnerUserIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        if (salesOwnerUserIds.Count == 0)
        {
            throw new ArgumentException("The demo workspace needs at least one sales owner.", nameof(salesOwnerUserIds));
        }

        var faker = new Faker()
        {
            Random = new Randomizer(SeedFor(customerRef)),
        };
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        var email = $"{DemoSeedConstants.EmailLocalPrefix}{customerRef}@{DemoSeedConstants.EmailDomain}";
        var phone = faker.Phone.PhoneNumber();
        var jobTitle = faker.Name.JobTitle();
        var companyName = faker.Company.CompanyName();

        var ownerUserId = salesOwnerUserIds[DemoIdentifiers.Bucket(customerRef, "owner", salesOwnerUserIds.Count)];
        var hasCompany = DemoIdentifiers.Bucket(customerRef, "company", 100) < DemoWorkspaceRules.CompanySharePercent;
        var companyId = hasCompany ? DemoIdentifiers.Derive(DemoSeedConstants.CompanyKind, customerRef) : (Guid?)null;
        var label = hasCompany ? companyName : $"{firstName} {lastName}";

        int? stageIndex = null;
        string? dealTitle = null;
        string? lostReason = null;
        if (DemoIdentifiers.Bucket(customerRef, "deal", 100) < DemoWorkspaceRules.DealSharePercent)
        {
            var stage = DemoWorkspaceRules.StageByBucket[DemoIdentifiers.Bucket(customerRef, "stage", DemoWorkspaceRules.StageByBucket.Count)];
            stageIndex = stage;
            var suffix = Text(Pick(DemoWorkspaceRules.DealSuffixKeys, customerRef, "deal-title"), locale);
            dealTitle = $"{label} - {suffix}";
            if (stage == DemoWorkspaceRules.LostStageIndex)
            {
                lostReason = Text(Pick(DemoWorkspaceRules.LostReasonKeys, customerRef, "lost-reason"), locale);
            }
        }

        string? taskTitle = null;
        DateTime? taskDueAt = null;
        var taskCompleted = false;
        if (DemoIdentifiers.Bucket(customerRef, "task", 100) < DemoWorkspaceRules.TaskSharePercent)
        {
            taskTitle = Text(Pick(DemoWorkspaceRules.TaskTitleKeys, customerRef, "task-title"), locale);
            var offsetDays = DemoIdentifiers.Bucket(customerRef, "due", DemoWorkspaceRules.TaskDueSpreadDays)
                - DemoWorkspaceRules.TaskDueCenterDays;
            var dueAt = now.AddDays(offsetDays).UtcDateTime;
            taskDueAt = dueAt;
            taskCompleted = dueAt < now.UtcDateTime;
        }

        string? noteBody = null;
        if (DemoIdentifiers.Bucket(customerRef, "note", 100) < DemoWorkspaceRules.NoteSharePercent)
        {
            noteBody = Text(Pick(DemoWorkspaceRules.NoteBodyKeys, customerRef, "note-body"), locale);
        }

        return new DemoContactProfile(
            DemoIdentifiers.Derive(DemoSeedConstants.ContactKind, customerRef),
            firstName,
            lastName,
            email,
            phone,
            jobTitle,
            ownerUserId,
            companyId,
            hasCompany ? companyName : null,
            stageIndex,
            dealTitle,
            lostReason,
            taskTitle,
            taskDueAt,
            taskCompleted,
            noteBody);
    }

    private static string Pick(IReadOnlyList<string> keys, string customerRef, string decision) =>
        keys[DemoIdentifiers.Bucket(customerRef, decision, keys.Count)];

    private static string Text(string key, string locale) => SharedTexts.Get(key, locale);

    private static int SeedFor(string customerRef)
    {
        if (!long.TryParse(customerRef, NumberStyles.None, CultureInfo.InvariantCulture, out var numeric))
        {
            throw new InvalidDataException("A customer id of the demo export is not numeric.");
        }

        return unchecked((int)(numeric ^ (numeric >> 32)));
    }
}
