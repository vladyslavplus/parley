using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Companies.BulkDelete;

public sealed class BulkDeleteCompaniesCommandValidator : AbstractValidator<BulkDeleteCompaniesCommand>
{
    public BulkDeleteCompaniesCommandValidator()
    {
        RuleFor(command => command.CompanyIds)
            .NotEmpty().WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.Count <= CrmLimits.BulkMaximumCount).WithErrorCode(ErrorCodes.ValidationFailed)
            .Must(ids => ids.All(id => id != Guid.Empty)).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
