using Mediator;
using Parley.Crm.Features.Deals.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Deals.Move;

/// <summary>
/// Moves a deal to a stage of its pipeline and to a place in that stage. Moving onto the won or lost stage closes the
/// deal, moving back to an open stage reopens it. <paramref name="Version"/> is the row version the client read; a stale
/// value returns 409 and the board rolls back.
/// </summary>
/// <param name="Id">Deal identifier.</param>
/// <param name="StageId">Target stage.</param>
/// <param name="Position">Zero-based place in the target stage; past the end means the last place.</param>
/// <param name="Version">Row version the client read.</param>
/// <param name="LostReason">Reason, kept when the target is the lost stage.</param>
[RequiresPermission(Permissions.DealsWrite)]
public sealed record MoveDealCommand(
    Guid Id,
    Guid StageId,
    int Position,
    uint Version,
    string? LostReason) : IRequest<Result<DealView>>;
