using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Companies.Create;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(command => command.Name)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.CompanyNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Domain)
            .MaximumLength(CrmLimits.DomainMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Country)
            .Length(CrmLimits.CountryCodeLength).WithErrorCode(ErrorCodes.ValidationFailed)
            .Matches("^[A-Za-z]{2}$").WithErrorCode(ErrorCodes.ValidationFailed)
            .When(command => !string.IsNullOrWhiteSpace(command.Country));
        RuleFor(command => command.Industry)
            .MaximumLength(CrmLimits.IndustryMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
