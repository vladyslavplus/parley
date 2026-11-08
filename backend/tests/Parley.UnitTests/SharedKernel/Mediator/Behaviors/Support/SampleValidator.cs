using FluentValidation;
using Parley.SharedKernel.Errors;

namespace Parley.UnitTests.SharedKernel.Mediator.Behaviors.Support;

public sealed class SampleValidator : AbstractValidator<SampleQuery>
{
    public SampleValidator()
    {
        RuleFor(query => query.Value)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.ValidationFailed);
    }
}
