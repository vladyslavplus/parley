using System.Runtime.CompilerServices;
using Parley.Crm.Persistence;

namespace Parley.Crm.Readers;

/// <summary>
/// Statement that returns the printable values of a set of contacts. The organization is the first predicate. Soft-deleted
/// contacts are included and flagged, so a campaign row of a deleted contact still reconciles with the aggregate; the send
/// path drops them. The owner predicate admits every contact when the all-owners flag is set; otherwise only contacts whose
/// owner is in the list. Parameters: organization, contact ids, owner ids, all-owners flag.
/// </summary>
public static class RecipientProfileSql
{
    private const string Contacts = "\"" + CrmSchemaNames.Schema + "\".\"" + CrmSchemaNames.ContactsTable + "\"";

    private const string Companies = "\"" + CrmSchemaNames.Schema + "\".\"" + CrmSchemaNames.CompaniesTable + "\"";

    private const string Statement = $$"""
        SELECT c."id" AS "contact_id",
               c."first_name" AS "first_name",
               c."last_name" AS "last_name",
               co."name" AS "company_name",
               (c."deleted_at" IS NOT NULL) AS "is_deleted"
        FROM {{Contacts}} c
        LEFT JOIN {{Companies}} co
               ON co."organization_id" = {0} AND co."id" = c."company_id" AND co."deleted_at" IS NULL
        WHERE c."organization_id" = {0}
          AND c."id" = ANY({1})
          AND ({3} OR c."owner_user_id" = ANY({2}))
        """;

    public static FormattableString Batch(Guid organizationId, Guid[] contactIds, Guid[] ownerUserIds, bool allOwners) =>
        FormattableStringFactory.Create(Statement, organizationId, contactIds, ownerUserIds, allOwners);
}
