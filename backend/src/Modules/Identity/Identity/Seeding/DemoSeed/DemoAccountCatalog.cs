using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Identity.Seeding.DemoSeed;

/// <summary>
/// Fixed demo workspace people: one admin, two teams with a manager and three reps each, one marketer and one
/// viewer. The order is part of the contract: the sales reps keep their position, so owners stay the same on every run.
/// </summary>
public static class DemoAccountCatalog
{
    /// <summary>Reserved top-level domain: demo addresses can never reach a real mailbox.</summary>
    public const string EmailDomain = "demo.parley.test";

    /// <summary>Length of generated passwords; the platform minimum is shorter.</summary>
    public const int PasswordLength = 20;

    public const string NorthTeamName = "Команда Північ";

    private const string SouthTeamName = "Команда Південь";

    public static IReadOnlyList<string> TeamNames { get; } = [NorthTeamName, SouthTeamName];

    public static IReadOnlyList<DemoAccountEntry> Accounts { get; } =
    [
        new("admin", "Олена Коваленко", SystemRoleSeed.AdminRoleId, SystemRoles.Admin, null),
        new("manager.north", "Андрій Шевченко", SystemRoleSeed.SalesManagerRoleId, SystemRoles.SalesManager, NorthTeamName),
        new("rep.north.1", "Ірина Мельник", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, NorthTeamName),
        new("rep.north.2", "Дмитро Ковальчук", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, NorthTeamName),
        new("rep.north.3", "Наталія Петренко", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, NorthTeamName),
        new("viewer", "Олег Ткаченко", SystemRoleSeed.ViewerRoleId, SystemRoles.Viewer, NorthTeamName),
        new("marketer", "Марія Бондаренко", SystemRoleSeed.MarketerRoleId, SystemRoles.Marketer, null),
        new("manager.south", "Тетяна Лисенко", SystemRoleSeed.SalesManagerRoleId, SystemRoles.SalesManager, SouthTeamName),
        new("rep.south.1", "Сергій Олійник", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, SouthTeamName),
        new("rep.south.2", "Юлія Гончар", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, SouthTeamName),
        new("rep.south.3", "Павло Савченко", SystemRoleSeed.SalesRepRoleId, SystemRoles.SalesRep, SouthTeamName),
    ];

    /// <summary>Sign-in address of a catalogue entry.</summary>
    public static string EmailOf(DemoAccountEntry entry) => $"{entry.LocalPart}@{EmailDomain}";
}
