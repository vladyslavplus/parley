using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Features.MlSettings.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.MlSettings.Update;

/// <summary>
/// Switches ML scoring on or off for the organization and, when asked, changes its active uplift model. Audited, and it
/// publishes the change through the outbox. Answered with 409 when the row changed since the client read it.
/// </summary>
/// <param name="MlEnabled">The new state of the switch.</param>
/// <param name="Version">Row version the client read; 0 when the organization had no row.</param>
/// <param name="UpliftChange">The new uplift model choice; null leaves the stored choice as it is.</param>
[RequiresPermission(Permissions.InsightsMlManage)]
public sealed record UpdateMlSettingsCommand(bool MlEnabled, uint Version, ActiveUpliftChange? UpliftChange) : IRequest<Result<MlSettingsView>>;
