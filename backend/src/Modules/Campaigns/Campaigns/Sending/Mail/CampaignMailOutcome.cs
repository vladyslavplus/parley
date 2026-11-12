namespace Parley.Campaigns.Sending.Mail;

/// <summary>What the sender learned about one message.</summary>
public enum CampaignMailOutcome
{
    /// <summary>The server accepted the message for delivery.</summary>
    Accepted,

    /// <summary>The connection or the login failed before the message was submitted. Nothing was sent, so a retry is safe.</summary>
    NotAccepted,

    /// <summary>The message was submitted, or the attempt failed in a way that may have submitted it. Never retried.</summary>
    Unknown,
}
