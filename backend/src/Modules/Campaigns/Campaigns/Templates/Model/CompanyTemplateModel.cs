namespace Parley.Campaigns.Templates.Model;

/// <summary>Company values a template can print.</summary>
/// <param name="Name">Company name; null when the contact has no company.</param>
public sealed record CompanyTemplateModel(string? Name);
