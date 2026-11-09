namespace Parley.Campaigns.Templates.Model;

/// <summary>Fixed values that stand in for a recipient in template previews.</summary>
public static class TemplateSampleData
{
    private const string FirstName = "Ada";

    private const string LastName = "Lovelace";

    private const string CompanyName = "Analytical Engine Ltd";

    private const string UnsubscribeUrl = "https://unsubscribe.example.test/sample";

    public static TemplateModel Model { get; } = new(
        new ContactTemplateModel(FirstName, LastName),
        new CompanyTemplateModel(CompanyName),
        UnsubscribeUrl);
}
