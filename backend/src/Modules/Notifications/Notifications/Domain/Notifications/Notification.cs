using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Notifications.Domain.Notifications;

/// <summary>
/// An in-app notification addressed to one user. It keeps the identifiers of the event as parameters, never rendered text:
/// the viewer's locale renders the text. The source event identifier, with the user and the type, is unique, so a replayed
/// event adds nothing.
/// </summary>
public sealed class Notification : BusinessEntity
{
    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    /// <summary>Identifiers and values the text needs, keyed by the parameter names of the inbox.</summary>
    public Dictionary<string, string> Parameters { get; set; } = [];

    /// <summary>Page the notification links to, one of the link route codes; null when it links nowhere.</summary>
    public string? LinkRoute { get; set; }

    public DateTime? ReadAt { get; set; }

    public Guid SourceEventId { get; set; }
}
