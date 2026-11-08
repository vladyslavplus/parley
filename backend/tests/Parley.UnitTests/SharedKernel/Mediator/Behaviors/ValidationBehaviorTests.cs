using AwesomeAssertions;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Mediator.Behaviors;
using Parley.SharedKernel.Results;
using Parley.UnitTests.SharedKernel.Mediator.Behaviors.Support;
using Xunit;

namespace Parley.UnitTests.SharedKernel.Mediator.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task RejectedValue_IsNotEchoedInFieldParameters()
    {
        var behavior = new ValidationBehavior<SampleQuery, Result<string>>(
            [new MinimumLengthValidator()]);
        var token = TestContext.Current.CancellationToken;

        var result = await behavior.Handle(
            new SampleQuery("s3cret"),
            (message, ct) => new ValueTask<Result<string>>(Result<string>.Success(message.Value!)),
            token);

        result.IsSuccess.Should().BeFalse();
        var field = result.Error!.FieldErrors!.Single();
        field.Field.Should().Be("Value");
        field.Parameters.Should().NotBeNull();
        field.Parameters!.Keys.Should().NotContain("PropertyValue");
        field.Parameters.Keys.Should().NotContain("PropertyName");
        field.Parameters.Values.Should().NotContain("s3cret",
            "a rejected password must never be echoed into ProblemDetails or stored idempotency responses");
        field.Parameters.Should().ContainKey("MinLength", "the rule limit stays available to clients");
    }

    [Fact]
    public async Task InvalidMessage_ReturnsValidationFailureWithFieldErrors()
    {
        var behavior = new ValidationBehavior<SampleQuery, Result<string>>(
            [new SampleValidator()]);
        var token = TestContext.Current.CancellationToken;

        var result = await behavior.Handle(
            new SampleQuery(string.Empty),
            (message, ct) => new ValueTask<Result<string>>(Result<string>.Success(message.Value!)),
            token);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(ErrorCodes.ValidationFailed);
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.FieldErrors.Should().NotBeNullOrEmpty();
        result.Error.FieldErrors!.Should().ContainSingle(field => field.Field == "Value");
    }

    [Fact]
    public async Task ValidMessage_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleQuery, Result<string>>(
            [new SampleValidator()]);
        var token = TestContext.Current.CancellationToken;

        var result = await behavior.Handle(
            new SampleQuery("value"),
            (message, ct) => new ValueTask<Result<string>>(Result<string>.Success(message.Value!)),
            token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("value");
    }
}
