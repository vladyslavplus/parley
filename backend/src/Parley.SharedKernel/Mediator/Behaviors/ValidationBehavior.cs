using FluentValidation;
using Mediator;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Results;

namespace Parley.SharedKernel.Mediator.Behaviors;

/// <summary>
/// Validates messages and returns field errors as a result failure.
/// </summary>
public sealed class ValidationBehavior<TMessage, TResponse>(
    IEnumerable<IValidator<TMessage>> validators) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : IResult<TResponse>
{
    private const string PropertyNamePlaceholder = "PropertyName";

    private const string PropertyValuePlaceholder = "PropertyValue";

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var fieldErrors = new List<FieldError>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(new ValidationContext<TMessage>(message), cancellationToken);
            foreach (var failure in result.Errors)
            {
                // Rule parameters only (limits, comparison values). The raw
                // property name and value are never echoed into params: a
                // rejected password, email or phone must not leak through
                // ProblemDetails and stored idempotency responses.
                var parameters = failure.FormattedMessagePlaceholderValues
                    .Where(entry => !string.Equals(entry.Key, PropertyNamePlaceholder, StringComparison.Ordinal)
                        && !string.Equals(entry.Key, PropertyValuePlaceholder, StringComparison.Ordinal))
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value?.ToString() ?? string.Empty);
                fieldErrors.Add(new FieldError(
                    failure.PropertyName,
                    string.IsNullOrWhiteSpace(failure.ErrorCode) ? ErrorCodes.ValidationFailed : failure.ErrorCode,
                    parameters.Count > 0 ? parameters : null));
            }
        }

        if (fieldErrors.Count > 0)
        {
            return TResponse.Failure(Error.ValidationWithFields(ErrorCodes.ValidationFailed, fieldErrors));
        }

        return await next(message, cancellationToken);
    }
}
