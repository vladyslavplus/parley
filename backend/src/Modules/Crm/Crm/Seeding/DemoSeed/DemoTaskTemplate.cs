namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>A demo task: the resource key of its title and the task type that fits the title.</summary>
/// <param name="TitleKey">Resource key of the task title.</param>
/// <param name="Type">One of the task types.</param>
public sealed record DemoTaskTemplate(string TitleKey, string Type);
