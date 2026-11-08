using FluentValidation;
using Parley.SharedKernel.Errors;

namespace Parley.UnitTests.SharedKernel.Mediator.Behaviors.Support;

public sealed class MinimumLengthValidator : AbstractValidator<SampleQuery>
{
    public MinimumLengthValidator()
    {
        RuleFor(query => query.Value)
            .MinimumLength(8)
            .WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
