using Mediator;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Create;

/// <summary>Creates a template. The subject and body must pass the template sandbox.</summary>
/// <param name="Name">Template name.</param>
/// <param name="SubjectLiquid">Subject source.</param>
/// <param name="BodyLiquid">Body source.</param>
/// <param name="Locale">Culture code the template is written in.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record CreateTemplateCommand(string Name, string SubjectLiquid, string BodyLiquid, string Locale) : IRequest<Result<TemplateView>>;
