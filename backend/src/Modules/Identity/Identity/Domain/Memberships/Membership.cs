using Parley.Identity.Contracts.Memberships;
using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Identity.Domain.Memberships;

/// <summary>
/// A user's membership in one organization: the role, the optional team, and the
/// active flag. Read on every request, so deactivation and role changes take effect
/// on the next request.
/// </summary>
public sealed class Membership : BusinessEntity
{
    public Membership()
    {
    }

    public Membership(Guid organizationId, Guid userId, Guid roleId, Guid? teamId, DateTime joinedAt)
        : base(organizationId)
    {
        UserId = userId;
        RoleId = roleId;
        TeamId = teamId;
        Status = MembershipStatuses.Active;
        JoinedAt = joinedAt;
    }

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public Guid? TeamId { get; set; }

    /// <summary>One of the values in <see cref="MembershipStatuses"/>.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }
}
