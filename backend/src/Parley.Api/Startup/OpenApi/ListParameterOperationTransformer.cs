using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Parley.Api.Controllers.Crm;
using Parley.Api.Controllers.Identity;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Deals;
using Parley.Crm.Shared.Pipelines;
using Parley.Crm.Shared.Sorting;
using Parley.Crm.Shared.Tasks;
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
    private const string ParameterStageId = "stageId";
    private const string ParameterContactId = "contactId";
    private const string ParameterCompanyId = "companyId";
    private const string ParameterOwnerUserId = "ownerUserId";
    private const string ParameterDealId = "dealId";
    private const string ParameterAssigneeUserId = "assigneeUserId";
    private const string ParameterType = "type";

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
        [ParameterStageId] = "Only deals on this stage (GUID).",
        [ParameterContactId] = "Only records of this contact (GUID).",
        [ParameterDealId] = "Only records of this deal (GUID).",
        [ParameterAssigneeUserId] = "Only tasks assigned to this member (GUID).",
        [ParameterType] = "Only timeline entries of this type.",
        [ParameterCompanyId] = "Only records of this company (GUID).",
        [ParameterOwnerUserId] = "Only records owned by this user (GUID).",
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> RouteDescriptions =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            [CrmRoutes.Deals] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ParameterStatus] = "Deal status to list: open, won or lost.",
            },
            [CrmRoutes.Tasks] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ParameterStatus] = "Task status to list: open, completed or overdue (open and past its due instant).",
            },
            [CrmRoutes.TasksMine] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ParameterStatus] = "Task status to list: open, completed or overdue (open and past its due instant).",
            },
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
        CrmRoutes.Pipelines,
        CrmRoutes.Deals,
        CrmRoutes.Tasks,
        CrmRoutes.TasksMine,
        CrmRoutes.Notes,
        CrmRoutes.Activities,
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
            [CrmRoutes.Pipelines] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = PipelineSortFields.Values,
                [ParameterOrder] = SortDirections.All,
            },
            [CrmRoutes.Deals] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = DealSortFields.Values,
                [ParameterOrder] = SortDirections.All,
                [ParameterStatus] = DealStatuses.All,
            },
            [CrmRoutes.Tasks] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = TaskSortFields.Values,
                [ParameterOrder] = SortDirections.All,
                [ParameterStatus] = TaskStatuses.All,
            },
            [CrmRoutes.TasksMine] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterSort] = TaskSortFields.Values,
                [ParameterOrder] = SortDirections.All,
                [ParameterStatus] = TaskStatuses.All,
            },
            [CrmRoutes.Notes] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterOrder] = SortDirections.All,
            },
            [CrmRoutes.Activities] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [ParameterOrder] = SortDirections.All,
                [ParameterType] = ActivityTypes.All,
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

            if (RouteDescriptions.TryGetValue(route, out var routeDescriptions) && routeDescriptions.TryGetValue(parameter.Name, out var routeDescription))
            {
                parameter.Description = routeDescription;
            }
            else if (Descriptions.TryGetValue(parameter.Name, out var description))
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
