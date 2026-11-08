namespace Parley.Crm.Domain.Tags;

/// <summary>
/// Link between a contact and a tag. Keyed by the organization and both identifiers, so a
/// link can exist only inside one organization.
/// </summary>
public sealed class ContactTag
{
    public ContactTag()
    {
    }

    public ContactTag(Guid organizationId, Guid contactId, Guid tagId)
    {
        OrganizationId = organizationId;
        ContactId = contactId;
        TagId = tagId;
    }

    public Guid OrganizationId { get; set; }

    public Guid ContactId { get; set; }

    public Guid TagId { get; set; }
}
