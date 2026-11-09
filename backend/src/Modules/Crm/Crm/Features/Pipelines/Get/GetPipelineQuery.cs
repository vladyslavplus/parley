using Mediator;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Get;

/// <summary>Reads one pipeline with its stages.</summary>
/// <param name="Id">Pipeline identifier.</param>
[RequiresPermission(Permissions.DealsRead)]
public sealed record GetPipelineQuery(Guid Id) : IRequest<Result<PipelineView>>;
