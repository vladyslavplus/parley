namespace Parley.SharedKernel.Results;

/// <summary>
/// A single field-level failure inside a validation error.
/// </summary>
public sealed record FieldError(string Field, string Code, IReadOnlyDictionary<string, string>? Parameters = null);
