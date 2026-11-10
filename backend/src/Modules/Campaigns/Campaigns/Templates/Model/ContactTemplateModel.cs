namespace Parley.Campaigns.Templates.Model;

/// <summary>Contact values a template can print.</summary>
/// <param name="FirstName">Contact first name.</param>
/// <param name="LastName">Contact last name; null when unknown.</param>
public sealed record ContactTemplateModel(string FirstName, string? LastName);
