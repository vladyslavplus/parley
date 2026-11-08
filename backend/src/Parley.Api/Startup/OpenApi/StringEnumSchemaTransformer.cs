using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Parley.Crm.Contracts.Search;
using Parley.Crm.Domain.Exports;
using Parley.Crm.Features.Contacts.Exports.Shared;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Shared.CustomFields;

namespace Parley.Api.Startup.OpenApi;

/// <summary>
/// Adds the allowed values to the string properties whose values come from constants (export status,
/// search kind, custom field type and entity type). The generated client then types them as unions
/// instead of plain strings. Each entry names a response type, its JSON property and the values.
/// </summary>
public sealed class StringEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, IReadOnlyList<string>>> EnumProperties =
        new Dictionary<Type, IReadOnlyDictionary<string, IReadOnlyList<string>>>
        {
            [typeof(ContactExportView)] = new Dictionary<string, IReadOnlyList<string>>
            {
                [JsonNames.Status] = ContactExportStatuses.All,
            },
            [typeof(SearchResultItem)] = new Dictionary<string, IReadOnlyList<string>>
            {
                [JsonNames.Kind] = SearchResultKinds.All,
            },
            [typeof(CustomFieldDefinitionView)] = new Dictionary<string, IReadOnlyList<string>>
            {
                [JsonNames.EntityType] = CustomFieldRules.EntityTypes,
                [JsonNames.FieldType] = CustomFieldRules.FieldTypes,
            },
        };

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (!EnumProperties.TryGetValue(context.JsonTypeInfo.Type, out var properties))
        {
            return Task.CompletedTask;
        }

        foreach (var (name, values) in properties)
        {
            if (schema.Properties is not null
                && schema.Properties.TryGetValue(name, out var property)
                && property is OpenApiSchema stringSchema)
            {
                stringSchema.Type = JsonSchemaType.String;
                stringSchema.Enum = [.. values.Select(JsonNode (value) => JsonValue.Create(value)!)];
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>JSON property names as the API serializes them (camel case).</summary>
    private static class JsonNames
    {
        public const string Status = "status";

        public const string Kind = "kind";

        public const string EntityType = "entityType";

        public const string FieldType = "fieldType";
    }
}
