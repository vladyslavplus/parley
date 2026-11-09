using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Pipelines;

/// <summary>
/// A sales pipeline: an ordered list of stages. Every organization has one default pipeline; the
/// unique index on the default flag keeps it to one.
/// </summary>
public sealed class Pipeline : BusinessEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Resource key of a system-provided name, shown in the request language; null for a name the user typed.</summary>
    public string? SystemKey { get; set; }

    public bool IsDefault { get; set; }

    public List<PipelineStage> Stages { get; set; } = [];

    /// <summary>
    /// Renames the pipeline. A system name becomes a typed name only when the new text differs from both the stored
    /// name and the name shown in the request language; keeping either text keeps the system key.
    /// </summary>
    public void Rename(string name, string displayName)
    {
        if (name == Name || name == displayName)
        {
            return;
        }

        Name = name;
        SystemKey = null;
    }
}
