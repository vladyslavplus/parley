using Mediator;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Preview;

/// <summary>Renders a template with fixed sample recipient data. Nothing is sent or stored.</summary>
/// <param name="Id">Template identifier.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record PreviewTemplateQuery(Guid Id) : IRequest<Result<TemplatePreviewView>>;
