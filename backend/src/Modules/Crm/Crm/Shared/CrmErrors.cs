using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Shared;

/// <summary>Builds the failures that name one request field.</summary>
public static class CrmErrors
{
    /// <summary>A validation failure of one field, carrying a stable code.</summary>
    public static Error Field(string field, string code) =>
        Error.ValidationWithFields(ErrorCodes.ValidationFailed, [new FieldError(field, code)]);
}
