using FluentValidation;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;

namespace Parley.Crm.Features.Contacts.Update;

public sealed class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        RuleFor(command => command.Version)
            .NotEqual(0U).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.FirstName)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithErrorCode(ErrorCodes.ValidationFailed)
            .MaximumLength(CrmLimits.PersonNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.LastName)
            .MaximumLength(CrmLimits.PersonNameMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Email)
            .MaximumLength(CrmLimits.EmailMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed)
            .EmailAddress().When(command => !string.IsNullOrWhiteSpace(command.Email)).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Phone)
            .MaximumLength(CrmLimits.PhoneMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.JobTitle)
            .MaximumLength(CrmLimits.JobTitleMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.City)
            .MaximumLength(CrmLimits.CityMaximumLength).WithErrorCode(ErrorCodes.ValidationFailed);
        RuleFor(command => command.Country)
            .Length(CrmLimits.CountryCodeLength).WithErrorCode(ErrorCodes.ValidationFailed)
            .Matches("^[A-Za-z]{2}$").WithErrorCode(ErrorCodes.ValidationFailed)
            .When(command => !string.IsNullOrWhiteSpace(command.Country));
    }
}
