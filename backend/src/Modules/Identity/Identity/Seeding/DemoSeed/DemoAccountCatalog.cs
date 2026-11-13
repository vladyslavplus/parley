using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Identity.Seeding.DemoSeed;

/// <summary>
/// Fixed demo workspace people: two admins (the shared local administrator and the plain local test account), two teams with
/// a manager and three reps each, one marketer and one viewer. The order is part of the contract: the sales reps keep their
/// position, so owners stay the same on every run.
/// </summary>
public static class DemoAccountCatalog
{
    /// <summary>Reserved top-level domain: demo addresses can never reach a real mailbox.</summary>
    public const string EmailDomain = "demo.parley.test";

    /// <summary>The plain local administrator used for quick manual checks; its address is a real domain on purpose.</summary>
    public const string LocalAdminEmail = "test@gmail.com";

    public const string NorthTeamName = "Команда Північ";

    private const string SouthTeamName = "Команда Південь";

    public static IReadOnlyList<string> TeamNames { get; } = [NorthTeamName, SouthTeamName];

    public static IReadOnlyList<DemoAccountEntry> Accounts { get; } =
    [
        new(LocalAdminEmail, "Тестовий адміністратор", SystemRoleSeed.AdminRoleId, SystemRoles.Admin, null),
        new("admin@" + EmailDomain, "Олена Коваленко", SystemRoleSeed.AdminRoleId, SystemRoles.Admin, null),
        new("manager.north@" + EmailDomain, "Андрій Шевченко", SystemRoleSeed.SalesManagerRoleId, SystemRoles.SalesManager, NorthTeamName),
        new("rep.north.1@" + EmailDomain, "Ірина Мельник", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, NorthTeamName),
        new("rep.north.2@" + EmailDomain, "Дмитро Ковальчук", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, NorthTeamName),
        new("rep.north.3@" + EmailDomain, "Наталія Петренко", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, NorthTeamName),
        new("viewer@" + EmailDomain, "Олег Ткаченко", SystemRoleSeed.ViewerRoleId, SystemRoles.Viewer, NorthTeamName),
        new("marketer@" + EmailDomain, "Марія Бондаренко", SystemRoleSeed.MarketerRoleId, SystemRoles.Marketer, null),
        new("manager.south@" + EmailDomain, "Тетяна Лисенко", SystemRoleSeed.SalesManagerRoleId, SystemRoles.SalesManager, SouthTeamName),
        new("rep.south.1@" + EmailDomain, "Сергій Олійник", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, SouthTeamName),
        new("rep.south.2@" + EmailDomain, "Юлія Гончар", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, SouthTeamName),
        new("rep.south.3@" + EmailDomain, "Павло Савченко", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, SouthTeamName),
    ];

    /// <summary>Sign-in address of a catalogue entry.</summary>
    public static string EmailOf(DemoAccountEntry entry) => entry.Email;
}
