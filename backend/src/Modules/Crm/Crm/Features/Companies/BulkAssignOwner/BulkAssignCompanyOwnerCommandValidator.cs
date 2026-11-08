using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Companies.BulkAssignOwner;

public sealed class BulkAssignCompanyOwnerCommandValidator : AbstractValidator<BulkAssignCompanyOwnerCommand>
{
    public BulkAssignCompanyOwnerCommandValidator()
    {
        RuleFor(command => command.CompanyIds)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.Count <= CrmLimits.BulkMaximumCount).WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.All(id => id != Guid.Empty)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.OwnerUserId)
            .NotEqual(Guid.Empty).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
