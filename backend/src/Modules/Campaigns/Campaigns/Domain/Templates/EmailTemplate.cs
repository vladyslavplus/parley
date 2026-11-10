using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Campaigns.Domain.Templates;

/// <summary>Reusable email content: a subject and a body written in the Liquid subset the template sandbox allows.</summary>
public sealed class EmailTemplate : BusinessEntity
{
    public string Name { get; set; } = string.Empty;

    public string SubjectLiquid { get; set; } = string.Empty;

    public string BodyLiquid { get; set; } = string.Empty;

    /// <summary>Culture code the template is written in.</summary>
    public string Locale { get; set; } = string.Empty;

    /// <summary>Set when the template is archived; archived templates stay referenced by past campaigns.</summary>
    public DateTime? ArchivedAt { get; set; }
}
