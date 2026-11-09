using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tags.Delete;

/// <summary>Deletes a tag and its attachments to contacts. Tags carry no history, so the delete is final.</summary>
/// <param name="Id">Tag identifier.</param>
[RequiresPermission(Permissions.TagsManage)]
public sealed record DeleteTagCommand(Guid Id) : IRequest<Result>;
