namespace Parley.Notifications.Domain.Recipients;

/// <summary>
/// Who is told about each event. Every rule returns the single addressee, or null when nobody is told. The actor of a change
/// is never told about their own change. An empty identifier, from an event written before the field existed, names no one.
/// </summary>
public static class NotificationRecipients
{
    public static Guid? ForTaskAssigned(Guid assigneeUserId, Guid actorUserId) => ExceptActor(assigneeUserId, actorUserId);

    public static Guid? ForDealAssigned(Guid ownerUserId, Guid actorUserId) => ExceptActor(ownerUserId, actorUserId);

    /// <summary>A reminder goes to the assignee even when they set the due date themselves: it is a reminder, not a notice of an action.</summary>
    public static Guid? ForTaskDueSoon(Guid assigneeUserId) => NonEmpty(assigneeUserId);

    public static Guid? ForCampaignCompleted(Guid? startedByUserId) => startedByUserId is { } starter ? NonEmpty(starter) : null;

    public static Guid? ForScoringRunCompleted(Guid? startedByUserId) => startedByUserId is { } starter ? NonEmpty(starter) : null;

    /// <summary>The inviter is told when the invited user becomes a member. The new member is never told about their own activation.</summary>
    public static Guid? ForMemberActivated(Guid activatedUserId, Guid invitedByUserId) => ExceptActor(invitedByUserId, activatedUserId);

    private static Guid? ExceptActor(Guid recipientUserId, Guid actorUserId) =>
        recipientUserId == actorUserId ? null : NonEmpty(recipientUserId);

    private static Guid? NonEmpty(Guid userId) => userId == Guid.Empty ? null : userId;
}
