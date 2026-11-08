namespace Parley.SharedKernel.Outbox.Consumers;

/// <summary>
/// Associates a consumer implementation with the DbContext that owns its inbox
/// table and its business rows. The dispatcher resolves that DbContext per
/// consumer at delivery time; a consumer without a registration is rejected so
/// its work can never be silently dropped.
/// </summary>
public sealed class ConsumerDbContextRegistration
{
    public required Type ConsumerType { get; init; }

    public required Type DbContextType { get; init; }
}
