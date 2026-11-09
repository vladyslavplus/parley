namespace Parley.Identity.Contracts.DemoSeed;

/// <summary>Outcome of the demo accounts seed. Counts and accounts only; no personal data beyond the demo addresses.</summary>
/// <param name="OrganizationCreated">True when this run created the organization.</param>
/// <param name="TeamsCreated">Teams created by this run.</param>
/// <param name="UsersCreated">Accounts created by this run.</param>
/// <param name="MembershipsCreated">Memberships created by this run.</param>
/// <param name="Accounts">Every demo account of the organization, in catalogue order.</param>
public sealed record IdentityDemoSeedResult(
    bool OrganizationCreated,
    int TeamsCreated,
    int UsersCreated,
    int MembershipsCreated,
    IReadOnlyList<DemoAccount> Accounts);
