using Mediator;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Templates.Update;

/// <summary>
/// Replaces the content of a template. The subject and body must pass the template sandbox. The version is the row
/// version the client read; a template changed since then answers 409.
/// </summary>
/// <param name="Id">Template identifier.</param>
/// <param name="Name">Template name.</param>
/// <param name="SubjectLiquid">Subject source.</param>
/// <param name="BodyLiquid">Body source.</param>
/// <param name="Locale">Culture code the template is written in.</param>
/// <param name="Version">Row version the client read.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record UpdateTemplateCommand(Guid Id, string Name, string SubjectLiquid, string BodyLiquid, string Locale, uint Version)
    : IRequest<Result<TemplateView>>;
