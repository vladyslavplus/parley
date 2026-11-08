using Mediator;
using Parley.Crm.Features.Tags.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tags.Create;

/// <summary>Creates a tag. Names are unique per organization, ignoring case.</summary>
/// <param name="Name">Tag name.</param>
/// <param name="ColorIndex">Palette index, from 0 to 4.</param>
[RequiresPermission(Permissions.TagsManage)]
public sealed record CreateTagCommand(string Name, short ColorIndex) : IRequest<Result<TagView>>;
