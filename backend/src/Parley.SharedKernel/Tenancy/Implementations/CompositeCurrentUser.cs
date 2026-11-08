using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.SharedKernel.Tenancy.Implementations;

/// <summary>
/// The single current user resolved by handlers and filters. The ambient job
/// user wins when set; otherwise the user comes from the HTTP request claim.
/// </summary>
public sealed class CompositeCurrentUser(
    JobCurrentUser ambient,
    IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var ambientUserId = ambient.UserId;
            if (ambientUserId.HasValue)
            {
                return ambientUserId;
            }

            var user = httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst(TenancyClaimTypes.UserId)?.Value;
            return Guid.TryParse(claim, out var userId) ? userId : null;
        }
    }

    public Guid RequireUserId() => UserId ?? throw new UserContextNotSetException();
}
