using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Parley.Notifications.Persistence;

/// <summary>Compares notification parameters by content, so change tracking sees an edited dictionary as a change.</summary>
public sealed class NotificationParametersComparer() : ValueComparer<Dictionary<string, string>>(
    (left, right) => left != null && right != null && left.Count == right.Count && !left.Except(right).Any(),
    parameters => parameters.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
    parameters => new Dictionary<string, string>(parameters));
