using FluentValidation;
using Parley.Identity.Shared.Validation;

namespace Parley.Identity.Features.Profile.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.DisplayName).DisplayName();
        RuleFor(command => command.Locale).SupportedLocale();
        RuleFor(command => command.TimeZone).TimeZone();
        RuleFor(command => command.Theme).Theme();
    }
}
