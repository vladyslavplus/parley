using Bogus;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>Builds valid contact bodies with unique emails, so tests never collide on the unique index.</summary>
public static class ContactFaker
{
    private const string EmailDomain = "contacts.parley.test";
    private const int LocalPartLength = 12;
    private const string CountryCode = "GB";

    private static readonly Faker<ContactBody> Contacts = new Faker<ContactBody>()
        .CustomInstantiator(faker => new ContactBody(
            FirstName: faker.Name.FirstName(),
            LastName: faker.Name.LastName(),
            Email: UniqueEmail(faker),
            Phone: null,
            JobTitle: null,
            Country: CountryCode,
            City: faker.Address.City(),
            CompanyId: null,
            OwnerUserId: null,
            CustomFields: null));

    public static ContactBody Create() => Contacts.Generate();

    public static ContactBody CreateOwnedBy(Guid ownerUserId) => Create() with { OwnerUserId = ownerUserId };

    public static string UniqueEmail(Faker faker) =>
        $"{faker.Random.AlphaNumeric(LocalPartLength).ToLowerInvariant()}@{EmailDomain}";
}
