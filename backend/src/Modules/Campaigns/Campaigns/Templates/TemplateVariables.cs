namespace Parley.Campaigns.Templates;

/// <summary>
/// The only variables a template may read. Each path is an exact member chain of <see cref="Model.TemplateModel"/>;
/// a bare object (for example <c>contact</c> alone) is not allowed because it would print the model type name.
/// </summary>
public static class TemplateVariables
{
    public const string ContactRoot = "contact";

    public const string CompanyRoot = "company";

    public const string UnsubscribeUrlRoot = "unsubscribe_url";

    public const string FirstNameMember = "first_name";

    public const string LastNameMember = "last_name";

    public const string CompanyNameMember = "name";

    public const string ContactFirstName = ContactRoot + "." + FirstNameMember;

    public const string ContactLastName = ContactRoot + "." + LastNameMember;

    public const string CompanyName = CompanyRoot + "." + CompanyNameMember;

    /// <summary>Loop variable of Liquid loops; its members are the loop counters.</summary>
    public const string LoopRoot = "forloop";

    public static IReadOnlySet<string> Allowed { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ContactFirstName,
        ContactLastName,
        CompanyName,
        UnsubscribeUrlRoot,
    };
}
