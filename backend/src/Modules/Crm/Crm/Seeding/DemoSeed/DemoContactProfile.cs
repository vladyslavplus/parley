namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Generated identity and workspace records of one demo contact. Built from the source customer id only, so a re-seed
/// gives the same values.
/// </summary>
/// <param name="Id">Identifier of the contact row.</param>
/// <param name="FirstName">Generated first name.</param>
/// <param name="LastName">Generated last name.</param>
/// <param name="Email">Address on the reserved demo domain.</param>
/// <param name="Phone">Generated phone number.</param>
/// <param name="JobTitle">Generated job title.</param>
/// <param name="OwnerUserId">Sales account that owns the contact and its generated records.</param>
/// <param name="CompanyId">Identifier of the company; null when the contact has no company.</param>
/// <param name="CompanyName">Generated company name; null without a company.</param>
/// <param name="DealStageIndex">Position of the deal stage in the default pipeline; null when the contact has no deal.</param>
/// <param name="DealTitle">Deal title; null without a deal.</param>
/// <param name="DealLostReason">Reason of a lost deal; null otherwise.</param>
/// <param name="TaskTitle">Task title; null without a task.</param>
/// <param name="TaskDueAt">Due instant of the task, UTC; null without a task.</param>
/// <param name="TaskCompleted">True when the task is complete.</param>
/// <param name="NoteBody">Note text; null without a note.</param>
public sealed record DemoContactProfile(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string JobTitle,
    Guid OwnerUserId,
    Guid? CompanyId,
    string? CompanyName,
    int? DealStageIndex,
    string? DealTitle,
    string? DealLostReason,
    string? TaskTitle,
    DateTime? TaskDueAt,
    bool TaskCompleted,
    string? NoteBody);
