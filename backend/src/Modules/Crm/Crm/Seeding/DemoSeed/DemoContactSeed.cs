namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>One generated demo contact with the workspace records derived from its archetype.</summary>
public sealed record DemoContactSeed(
    int Index,
    DemoArchetype Archetype,
    Guid Id,
    string CustomerRef,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string JobTitle,
    string? Country,
    string? City,
    Guid OwnerUserId,
    Guid? CompanyId,
    string? CompanyName,
    int? DealStageIndex,
    string? DealTitle,
    string? DealLostReason,
    DateTime? DealExpectedCloseOn,
    string? TaskTitle,
    DateTime? TaskDueAt,
    bool TaskCompleted,
    string? NoteBody);
