using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Features.MlSettings.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.MlSettings.Get;

/// <summary>Reads the ML switch of the organization.</summary>
[RequiresPermission(Permissions.InsightsMlManage)]
public sealed record GetMlSettingsQuery : IRequest<Result<MlSettingsView>>;
