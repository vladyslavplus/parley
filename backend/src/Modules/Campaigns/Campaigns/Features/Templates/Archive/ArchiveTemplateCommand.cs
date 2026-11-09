using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Archive;

/// <summary>Archives a template: it leaves the library, while past campaigns keep their reference.</summary>
/// <param name="Id">Template identifier.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record ArchiveTemplateCommand(Guid Id) : IRequest<Result>;
