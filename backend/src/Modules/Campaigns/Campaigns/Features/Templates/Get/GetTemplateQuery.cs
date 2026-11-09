using Mediator;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Get;

/// <summary>Reads one template with its source.</summary>
/// <param name="Id">Template identifier.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record GetTemplateQuery(Guid Id) : IRequest<Result<TemplateView>>;
