using AwesomeAssertions;
using Parley.Notifications.Domain.Preferences;
using Parley.Notifications.Domain.Recipients;
using Xunit;

namespace Parley.UnitTests.Notifications;

public sealed class NotificationRecipientsTests
{
    [Fact]
    public void Assignment_TellsTheAssignee_ButNeverTheActorOfTheirOwnChange()
    {
        var assignee = Guid.CreateVersion7();
        var actor = Guid.CreateVersion7();

        NotificationRecipients.ForTaskAssigned(assignee, actor).Should().Be(assignee);
        NotificationRecipients.ForTaskAssigned(actor, actor).Should().BeNull("a self-assignment notifies nobody");
        NotificationRecipients.ForDealAssigned(assignee, actor).Should().Be(assignee);
        NotificationRecipients.ForDealAssigned(actor, actor).Should().BeNull();
    }

    [Fact]
    public void DueSoonReminder_GoesToTheAssignee_EvenWhenTheyAssignedItToThemselves()
    {
        var assignee = Guid.CreateVersion7();

        NotificationRecipients.ForTaskDueSoon(assignee).Should().Be(assignee);
    }

    [Fact]
    public void EmptyIdentifier_NamesNobody()
    {
        NotificationRecipients.ForTaskDueSoon(Guid.Empty).Should().BeNull("an event written before the field existed names no one");
        NotificationRecipients.ForCampaignCompleted(Guid.Empty).Should().BeNull();
    }

    [Fact]
    public void CampaignCompletion_GoesToTheStarter_AndNobodyWhenNoStarterWasRecorded()
    {
        var starter = Guid.CreateVersion7();

        NotificationRecipients.ForCampaignCompleted(starter).Should().Be(starter);
        NotificationRecipients.ForCampaignCompleted(null).Should().BeNull();
        NotificationRecipients.ForScoringRunCompleted(null).Should().BeNull();
    }

    [Fact]
    public void MemberActivation_TellsTheInviter_AndNeverTheNewMemberAboutTheirOwnActivation()
    {
        var inviter = Guid.CreateVersion7();
        var invited = Guid.CreateVersion7();

        NotificationRecipients.ForMemberActivated(invited, inviter).Should().Be(inviter);
        NotificationRecipients.ForMemberActivated(inviter, inviter).Should().BeNull();
    }

    [Fact]
    public void Preference_WithNoStoredSwitch_IsOn_AndAStoredOffSwitchIsOff()
    {
        NotificationPreferenceRules.IsEnabled(null).Should().BeTrue("a type without a stored switch notifies");
        NotificationPreferenceRules.IsEnabled(false).Should().BeFalse();
        NotificationPreferenceRules.IsEnabled(true).Should().BeTrue();
    }
}
