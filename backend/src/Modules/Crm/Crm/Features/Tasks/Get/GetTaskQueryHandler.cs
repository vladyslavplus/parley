using Mediator;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Tasks;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.Get;

public sealed class GetTaskQueryHandler(CrmDbContext dbContext, IDataScope dataScope, OwnerDirectory ownerDirectory)
    : IRequestHandler<GetTaskQuery, Result<TaskView>>
{
    public async ValueTask<Result<TaskView>> Handle(GetTaskQuery query, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.TasksRead, cancellationToken);
        var row = await TaskLookup.FindRowAsync(dbContext, dataScope, query.Id, scope, cancellationToken);
        if (row is null)
        {
            return Result<TaskView>.Failure(Error.NotFound(CrmErrorCodes.TaskNotFound));
        }

        return Result<TaskView>.Success(await TaskViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
