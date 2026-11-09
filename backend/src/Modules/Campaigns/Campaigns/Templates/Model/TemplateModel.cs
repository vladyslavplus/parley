namespace Parley.Campaigns.Templates.Model;

/// <summary>
/// The only object a template can read. It carries exactly the values in <see cref="TemplateVariables"/>, so a template
/// never reaches services, configuration or other domain data.
/// </summary>
/// <param name="Contact">The recipient contact values.</param>
/// <param name="Company">The recipient company values.</param>
/// <param name="UnsubscribeUrl">Link the recipient follows to stop receiving campaigns. Passed raw: the render encodes it with the encoder of the body.</param>
public sealed record TemplateModel(ContactTemplateModel Contact, CompanyTemplateModel Company, string UnsubscribeUrl);
