using Mediator;
using Parley.Crm.Features.Tags.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tags.Update;

/// <summary>Renames a tag and changes its color. Contacts keep their attachments.</summary>
/// <param name="Id">Tag identifier.</param>
/// <param name="Name">New tag name.</param>
/// <param name="ColorIndex">New palette index, from 0 to 4.</param>
[RequiresPermission(Permissions.TagsManage)]
public sealed record UpdateTagCommand(Guid Id, string Name, short ColorIndex) : IRequest<Result<TagView>>;
