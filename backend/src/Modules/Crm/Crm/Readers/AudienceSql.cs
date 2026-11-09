using System.Runtime.CompilerServices;
using Parley.Crm.Persistence;

namespace Parley.Crm.Readers;

/// <summary>
/// Keyset batch statement of the email-reachable contacts with their tags and last order instant. Tags come from a
/// correlated array in the same statement, so a batch costs one round trip. Parameters: organization, after contact
/// id, limit.
/// </summary>
public static class AudienceSql
{
    private const string Contacts = "\"" + CrmSchemaNames.Schema + "\".\"" + CrmSchemaNames.ContactsTable + "\"";

    private const string ContactTags = "\"" + CrmSchemaNames.Schema + "\".\"" + CrmSchemaNames.ContactTagsTable + "\"";

    private const string Statement = $$"""
        SELECT c."id" AS "contact_id",
               c."owner_user_id" AS "owner_user_id",
               c."country" AS "country_iso2",
               c."last_order_at" AS "last_order_at",
               c."email" AS "email",
               c."email_normalized" AS "email_normalized",
               ARRAY(
                   SELECT ct."tag_id"
                   FROM {{ContactTags}} ct
                   WHERE ct."organization_id" = {0}
                     AND ct."contact_id" = c."id"
               ) AS "tag_ids"
        FROM {{Contacts}} c
        WHERE c."organization_id" = {0}
          AND c."deleted_at" IS NULL
          AND c."email_normalized" IS NOT NULL
          AND ({1}::uuid IS NULL OR c."id" > {1}::uuid)
        ORDER BY c."id"
        LIMIT {2}
        """;

    public static FormattableString Batch(Guid organizationId, Guid? afterContactId, int limit) =>
        FormattableStringFactory.Create(Statement, organizationId, afterContactId, limit);
}
