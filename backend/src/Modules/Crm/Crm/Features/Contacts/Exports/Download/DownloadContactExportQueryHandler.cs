using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Exports;
using Parley.Crm.Exports;
using Parley.Crm.Features.Contacts.Exports.Shared;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Contacts.Exports.Download;

public sealed class DownloadContactExportQueryHandler(
    CrmDbContext dbContext,
    ICurrentUser currentUser,
    ContactExportStorage storage,
    TimeProvider timeProvider) : IRequestHandler<DownloadContactExportQuery, Result<ContactExportFile>>
{
    public async ValueTask<Result<ContactExportFile>> Handle(DownloadContactExportQuery query, CancellationToken cancellationToken)
    {
        var requester = currentUser.RequireUserId();
        var export = await dbContext.ContactExports.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == query.Id && row.RequestedByUserId == requester, cancellationToken);
        if (export is null)
        {
            return Result<ContactExportFile>.Failure(Error.NotFound(CrmErrorCodes.ContactExportNotFound));
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (export.ExpiresAt <= now)
        {
            return Result<ContactExportFile>.Failure(Error.NotFound(CrmErrorCodes.ContactExportExpired));
        }

        if (export.Status != ContactExportStatuses.Completed || export.FileName is null)
        {
            return Result<ContactExportFile>.Failure(Error.Conflict(CrmErrorCodes.ContactExportNotReady));
        }

        var path = storage.PathFor(export.FileName);
        if (!File.Exists(path))
        {
            return Result<ContactExportFile>.Failure(Error.NotFound(CrmErrorCodes.ContactExportExpired));
        }

        return Result<ContactExportFile>.Success(new ContactExportFile(path, ContactExportFiles.DownloadName));
    }
}
