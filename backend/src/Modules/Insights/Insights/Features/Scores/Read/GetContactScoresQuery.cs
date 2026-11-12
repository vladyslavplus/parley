using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Scores.Read;

/// <summary>
/// The scores of one contact the caller may see. Everyone with the insights view sees the chips: the RFM segment, the uplift
/// segment, and which model and provenance produced the scores. Only a caller with the ML view also sees the numbers and the
/// payload. The read is from the database only: no call to the scoring service is made in a user request.
/// </summary>
[RequiresPermission(Permissions.InsightsView)]
public sealed record GetContactScoresQuery(Guid ContactId) : IRequest<Result<ContactScoresView>>;
