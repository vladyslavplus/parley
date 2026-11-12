namespace Parley.Campaigns.Templates.Model;

/// <summary>Fixed values that stand in for a recipient in template previews and test sends.</summary>
public static class TemplateSampleData
{
    private const string FirstName = "Ada";

    private const string LastName = "Lovelace";

    private const string CompanyName = "Analytical Engine Ltd";

    private const string PreviewUnsubscribeUrl = "https://unsubscribe.example.test/sample";

    /// <summary>
    /// Token of the sample links of a test send. No recipient is ever issued it, so the links are refused as invalid if followed.
    /// </summary>
    public const string UnsubscribeToken = "sample";

    /// <summary>Sample model for previews. Its unsubscribe link is a fixed example address, not a working link.</summary>
    public static TemplateModel Model { get; } = WithUnsubscribeUrl(PreviewUnsubscribeUrl);

    /// <summary>Sample model whose unsubscribe link is the given address, for test sends.</summary>
    public static TemplateModel WithUnsubscribeUrl(string unsubscribeUrl) => new(
        new ContactTemplateModel(FirstName, LastName),
        new CompanyTemplateModel(CompanyName),
        unsubscribeUrl);
}
