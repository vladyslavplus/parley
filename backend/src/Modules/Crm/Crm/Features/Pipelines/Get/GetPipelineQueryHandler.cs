using Mediator;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Pipelines;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Get;

public sealed class GetPipelineQueryHandler(CrmDbContext dbContext) : IRequestHandler<GetPipelineQuery, Result<PipelineView>>
{
    public async ValueTask<Result<PipelineView>> Handle(GetPipelineQuery query, CancellationToken cancellationToken)
    {
        var pipeline = await PipelineReader.ReadAsync(dbContext, query.Id, cancellationToken);
        return pipeline is null
            ? Result<PipelineView>.Failure(Error.NotFound(CrmErrorCodes.PipelineNotFound))
            : Result<PipelineView>.Success(pipeline);
    }
}
