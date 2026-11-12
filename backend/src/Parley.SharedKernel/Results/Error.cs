namespace Parley.SharedKernel.Results;

/// <summary>
/// A failure value paired with a stable error code. The code format is
/// <c>module.area.reason</c>, for example <c>crm.contact.not_found</c>.
/// </summary>
public sealed record Error(
    string Code,
    ErrorType Type,
    IReadOnlyDictionary<string, string>? Parameters = null,
    IReadOnlyList<FieldError>? FieldErrors = null)
{
    public static Error Validation(string code, IReadOnlyDictionary<string, string>? parameters = null) =>
        new(code, ErrorType.Validation, parameters);

    public static Error ValidationWithFields(string code, IReadOnlyList<FieldError> fieldErrors) =>
        new(code, ErrorType.Validation, null, fieldErrors);

    public static Error Unauthorized(string code) => new(code, ErrorType.Unauthorized);

    public static Error Forbidden(string code) => new(code, ErrorType.Forbidden);

    public static Error NotFound(string code) => new(code, ErrorType.NotFound);

    public static Error Conflict(string code) => new(code, ErrorType.Conflict);

    public static Error RateLimited(string code) => new(code, ErrorType.RateLimited);

    public static Error Failure(string code) => new(code, ErrorType.Failure);
}
