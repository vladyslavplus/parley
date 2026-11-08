namespace Parley.SharedKernel.Outbox.Mapping;

/// <summary>
/// Schema and table constants for the transactional outbox. Each module maps
/// the shared rows into its own schema through
/// <see cref="OutboxModelBuilderExtensions.AddOutboxConventions"/>.
/// </summary>
public static class OutboxTableNames
{
    public const string OutboxMessages = "outbox_messages";

    public const string InboxMessages = "inbox_messages";
}
