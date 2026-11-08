using Microsoft.EntityFrameworkCore;
using Parley.Identity.Persistence;

namespace Parley.Identity.Features.Teams.Shared;

/// <summary>Team name rules shared by create and rename.</summary>
internal static class TeamNames
{
    public static Task<bool> IsTakenAsync(IdentityDbContext dbContext, string name, Guid? excludedTeamId, CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        return dbContext.Teams.AnyAsync(
            team => team.NameNormalized == normalized && (excludedTeamId == null || team.Id != excludedTeamId.Value),
            cancellationToken);
    }
}
