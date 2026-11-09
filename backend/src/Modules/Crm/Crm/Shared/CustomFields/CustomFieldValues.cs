using System.Globalization;
using System.Text.Json;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.CustomFields;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Shared.CustomFields;

/// <summary>
/// Validates submitted custom field values against the active definitions of a record type
/// and merges them into the stored JSON object. Pure logic, so every rule is unit tested.
/// </summary>
public static class CustomFieldValues
{
    private const string FieldPathPrefix = "customFields.";

    /// <summary>The map a create submits when the client sent no custom fields: every required field must then be present.</summary>
    public static IReadOnlyDictionary<string, JsonElement> NoValues { get; } = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    /// <summary>
    /// Returns the merged JSON object. <paramref name="provided"/> null keeps the stored active
    /// values; a non-null map replaces them. Stored values of archived definitions are always kept.
    /// Failures carry one field error per invalid or missing value.
    /// </summary>
    public static Result<string> Merge(
        IReadOnlyList<CustomFieldDefinition> activeDefinitions,
        IReadOnlyDictionary<string, JsonElement>? provided,
        string storedJson)
    {
        ArgumentNullException.ThrowIfNull(activeDefinitions);
        ArgumentNullException.ThrowIfNull(storedJson);

        var stored = ToMap(storedJson);
        var active = activeDefinitions.ToDictionary(definition => definition.Key, StringComparer.Ordinal);
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (key, value) in stored.Where(entry => !active.ContainsKey(entry.Key)))
        {
            result[key] = value;
        }

        var fieldErrors = new List<FieldError>();
        if (provided is null)
        {
            foreach (var (key, value) in stored.Where(entry => active.ContainsKey(entry.Key)))
            {
                result[key] = value;
            }
        }
        else
        {
            foreach (var (key, value) in provided)
            {
                if (!active.TryGetValue(key, out var definition))
                {
                    fieldErrors.Add(Field(key, CrmErrorCodes.CustomFieldUnknownKey));
                    continue;
                }

                if (value.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }

                var error = ValidateValue(definition, value);
                if (error is not null)
                {
                    fieldErrors.Add(Field(key, error));
                    continue;
                }

                result[key] = value.Clone();
            }
        }

        // A null map means "leave the values alone", so a required field the record never had is not
        // the caller's fault. Only a submitted map (create passes an empty one) must satisfy the requirement.
        if (provided is not null)
        {
            foreach (var definition in activeDefinitions.Where(definition => definition.IsRequired))
            {
                if (!result.ContainsKey(definition.Key))
                {
                    fieldErrors.Add(Field(definition.Key, CrmErrorCodes.CustomFieldRequired));
                }
            }
        }

        if (fieldErrors.Count > 0)
        {
            return Result<string>.Failure(Error.ValidationWithFields(ErrorCodes.ValidationFailed, fieldErrors));
        }

        return Result<string>.Success(JsonSerializer.Serialize(result));
    }

    /// <summary>Reads a stored JSON object into a map. A malformed or non-object value reads as empty.</summary>
    public static Dictionary<string, JsonElement> ToMap(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }
    }

    private static string? ValidateValue(CustomFieldDefinition definition, JsonElement value) => definition.FieldType switch
    {
        CustomFieldTypes.Text => ValidateText(value),
        CustomFieldTypes.Number => value.ValueKind == JsonValueKind.Number ? null : CrmErrorCodes.CustomFieldTypeMismatch,
        CustomFieldTypes.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : CrmErrorCodes.CustomFieldTypeMismatch,
        CustomFieldTypes.Date => ValidateDate(value),
        CustomFieldTypes.Select => ValidateSelect(definition, value),
        _ => CrmErrorCodes.CustomFieldTypeMismatch,
    };

    private static string? ValidateText(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return CrmErrorCodes.CustomFieldTypeMismatch;
        }

        return value.GetString()!.Length > CrmLimits.CustomFieldTextMaximumLength ? CrmErrorCodes.CustomFieldTextTooLong : null;
    }

    private static string? ValidateDate(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return CrmErrorCodes.CustomFieldTypeMismatch;
        }

        return DateOnly.TryParseExact(value.GetString(), CustomFieldRules.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? null
            : CrmErrorCodes.CustomFieldValueInvalid;
    }

    private static string? ValidateSelect(CustomFieldDefinition definition, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return CrmErrorCodes.CustomFieldTypeMismatch;
        }

        var options = JsonSerializer.Deserialize<List<string>>(definition.Options) ?? [];
        return options.Contains(value.GetString()!, StringComparer.Ordinal) ? null : CrmErrorCodes.CustomFieldOptionInvalid;
    }

    private static FieldError Field(string key, string code) => new(FieldPathPrefix + key, code);
}
