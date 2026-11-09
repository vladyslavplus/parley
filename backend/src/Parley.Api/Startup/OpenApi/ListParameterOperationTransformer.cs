using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Parley.Api.Controllers.Crm;
using Parley.Api.Controllers.Identity;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Sorting;
using Parley.Identity.Features.Audit.Shared;
using Parley.Identity.Features.Invitations.Shared;
using Parley.Identity.Features.Members.Shared;
using Parley.Identity.Features.Teams.Shared;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Startup.OpenApi;

/// <summary>
/// Describes the query parameters of the list endpoints. Every parameter gets its description, and the
/// sort, order, status and type filters get their allowed values as an enum, so generated clients type them
/// instead of accepting any string. The values come from the same constants the validators check.
/// </summary>
public sealed class ListParameterOperationTransformer : IOpenApiOperationTransformer
{
    private const string ParameterAfter = "after";
    private const string ParameterLimit = "limit";
    private const string ParameterQ = "q";
    private const string ParameterSort = "sort";
    private const string ParameterOrder = "order";
    private const string ParameterStatus = "status";
    private const string ParameterRoleCode = "roleCode";
    private const string ParameterTeamId = "teamId";
    private const string ParameterAction = "action";
    private const string ParameterActorUserId = "actorUserId";
    private const string ParameterFrom = "from";
    private const string ParameterTo = "to";
    private const string ParameterEntityType = "entityType";
    private const string ParameterFieldType = "fieldType";

    private const string SegmentSeparator = "/";

    private static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ParameterAfter] = "Cursor of the page to continue from, exactly as the previous page returned it.",
        [ParameterLimit] = "Page size. Defaults to 20 and is capped at 100.",
        [ParameterQ] = "Case-insensitive text the listed fields must contain.",
        [ParameterSort] = "Field to sort by. The allowed values are listed per endpoint.",
        [ParameterOrder] = "Sort direction: asc or desc.",
        [ParameterStatus] = "Membership status to list: active (default), deactivated or all.",
        [ParameterRoleCode] = "Only rows with this role code.",
        [ParameterTeamId] = "Only members of this team (GUID).",
        [ParameterAction] = "Only events with this exact action code.",
        [ParameterActorUserId] = "Only events of this actor (GUID).",
        [ParameterFrom] = "First instant included, UTC, ISO 8601. A value without an offset is read as UTC.",
        [ParameterTo] = "Instant excluded, UTC, ISO 8601.",
        [ParameterEntityType] = "Only definitions of this record type.",
        [ParameterFieldType] = "Only definitions of this field type.",
    };

    private static readonly string[] ListRoutes =
    [
        IdentityRoutes.Members,
        IdentityRoutes.Invitations,
        IdentityRoutes.Teams,
        IdentityRoutes.AuditEvents,
        CrmRoutes.Contacts,
        CrmRoutes.Companies,
        CrmRoutes.Tags,
        CrmRoutes.CustomFields,
        CrmRoutes.Owners,
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> AllowedValuesByRoute =
        new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>(StringComparer.Ordinal)
        {
            [IdentityRoutes.Members] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = MemberListRules.SortFields,
                [ParameterOrder] = SortDirections.All,
                [ParameterStatus] = MemberListRules.Statuses,
            },
            [IdentityRoutes.Invitations] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = InvitationListRules.SortFields,
                [ParameterOrder] = SortDirections.All,
            },
            [IdentityRoutes.Teams] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = TeamListRules.SortFields,
                [ParameterOrder] = SortDirections.All,
            },
            [IdentityRoutes.AuditEvents] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = AuditListRules.SortFields,
                [ParameterOrder] = SortDirections.All,
            },
            [CrmRoutes.Contacts] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = ListSortFields.Values,
                [ParameterOrder] = SortDirections.All,
            },
            [CrmRoutes.Companies] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = ListSortFields.Values,
                [ParameterOrder] = SortDirections.All,
            },
            [CrmRoutes.CustomFields] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = CustomFieldListRules.SortFields,
                [ParameterOrder] = SortDirections.All,
                [ParameterEntityType] = CustomFieldRules.EntityTypes,
                [ParameterFieldType] = CustomFieldRules.FieldTypes,
            },
        };

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var route = ListRouteOf(context.Description.RelativePath);
        if (route is null)
        {
            return Task.CompletedTask;
        }

        var allowedValues = AllowedValuesByRoute.GetValueOrDefault(route);
        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parameter.Name is null)
            {
                continue;
            }

            if (Descriptions.TryGetValue(parameter.Name, out var description))
            {
                parameter.Description = description;
            }

            if (allowedValues is not null && allowedValues.TryGetValue(parameter.Name, out var values))
            {
                parameter.Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Enum = [.. values.Select(JsonNode (value) => JsonValue.Create(value)!)],
                };
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>The list route an operation path belongs to, or null for any other endpoint.</summary>
    private static string? ListRouteOf(string? relativePath)
    {
        var path = relativePath?.Trim('/') ?? string.Empty;
        return ListRoutes.FirstOrDefault(route =>
            string.Equals(path, route, StringComparison.Ordinal)
            || path.EndsWith(SegmentSeparator + route, StringComparison.Ordinal));
    }
}
