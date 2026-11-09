using AwesomeAssertions;
using Parley.Crm.Seeding.DemoSeed;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.UnitTests.Crm.Seeding;

/// <summary>The generated workspace texts follow the organization locale, and the same customer always gets the same record.</summary>
public sealed class DemoContactProfilesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);

    private static readonly Guid[] Owners = [Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7()];

    [Fact]
    public void The_same_customer_gives_the_same_record()
    {
        var first = DemoContactProfiles.Create("1234", Owners, Now, CultureCodes.Ukrainian);
        var second = DemoContactProfiles.Create("1234", Owners, Now, CultureCodes.Ukrainian);

        second.Should().Be(first);
    }

    [Fact]
    public void Generated_task_and_note_texts_follow_the_locale()
    {
        var profile = FindCustomerWithTaskAndNote();

        var ukrainian = DemoContactProfiles.Create(profile, Owners, Now, CultureCodes.Ukrainian);
        var english = DemoContactProfiles.Create(profile, Owners, Now, CultureCodes.English);

        ukrainian.TaskTitle!.Any(IsCyrillic).Should().BeTrue();
        english.TaskTitle!.Any(IsCyrillic).Should().BeFalse();
        ukrainian.NoteBody!.Any(IsCyrillic).Should().BeTrue();
        english.NoteBody!.Any(IsCyrillic).Should().BeFalse();
        ukrainian.TaskTitle.Should().NotBe(english.TaskTitle);
    }

    [Fact]
    public void Owners_are_chosen_from_the_given_accounts_only()
    {
        var profile = DemoContactProfiles.Create("777", Owners, Now, CultureCodes.Ukrainian);

        Owners.Should().Contain(profile.OwnerUserId);
    }

    private static string FindCustomerWithTaskAndNote()
    {
        for (var index = 1000; index < 1400; index++)
        {
            var reference = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var profile = DemoContactProfiles.Create(reference, Owners, Now, CultureCodes.Ukrainian);
            if (profile.TaskTitle is not null && profile.NoteBody is not null)
            {
                return reference;
            }
        }

        throw new InvalidOperationException("No customer in the test range has both a task and a note.");
    }

    private static bool IsCyrillic(char character) => character is >= 'Ѐ' and <= 'ӿ';
}
