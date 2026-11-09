using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Results;

namespace Parley.SharedKernel.Paging;

/// <summary>Validation errors of the shared list parameters (cursor, sort, order and filters).</summary>
public static class PagingErrors
{
    /// <summary>A validation failure of one query parameter, carrying the stable validation code.</summary>
    public static Error Field(string field) =>
        Error.ValidationWithFields(ErrorCodes.ValidationFailed, [new FieldError(field, ErrorCodes.ValidationFailed)]);
}
