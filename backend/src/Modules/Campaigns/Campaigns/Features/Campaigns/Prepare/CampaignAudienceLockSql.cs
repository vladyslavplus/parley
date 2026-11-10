using System.Runtime.CompilerServices;

namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>
/// Session advisory lock of one campaign's freeze. The key is two int4 values: a hash of a fixed namespace name and a hash
/// of the campaign id, so it cannot collide with other advisory locks of the database. A session lock belongs to its
/// database connection, so the freezer takes it on one open connection and releases it before that connection closes.
/// A run that dies releases the lock when its connection ends.
/// </summary>
public static class CampaignAudienceLockSql
{
    private const string LockNamespace = "campaigns.audience-freeze";

    private const string TryAcquireStatement = "SELECT pg_try_advisory_lock(hashtext({0}), hashtext({1})) AS \"Value\"";

    private const string ReleaseStatement = "SELECT pg_advisory_unlock(hashtext({0}), hashtext({1})) AS \"Value\"";

    public static FormattableString TryAcquire(Guid campaignId) =>
        FormattableStringFactory.Create(TryAcquireStatement, LockNamespace, campaignId.ToString("D"));

    public static FormattableString Release(Guid campaignId) =>
        FormattableStringFactory.Create(ReleaseStatement, LockNamespace, campaignId.ToString("D"));
}
