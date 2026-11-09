namespace Parley.Campaigns.Templates;

/// <summary>Thrown by <see cref="TemplateOutputWriter"/> when a render writes more than the output limit.</summary>
public sealed class TemplateOutputLimitException(int limit) : Exception("The template output exceeds its limit.")
{
    public int Limit { get; } = limit;
}
