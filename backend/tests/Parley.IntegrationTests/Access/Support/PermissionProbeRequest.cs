using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.IntegrationTests.Access.Support;

[RequiresPermission(Permissions.ContactsWrite)]
public sealed record PermissionProbeRequest : IRequest<Result>;
