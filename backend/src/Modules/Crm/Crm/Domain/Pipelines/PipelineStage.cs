using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Pipelines;

/// <summary>
/// One stage of a pipeline. Open stages come first in position order; the won and lost stages close
/// the pipeline. Names are unique per pipeline ignoring case.
/// </summary>
public sealed class PipelineStage : BusinessEntity
{
    public Guid PipelineId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Resource key of a system-provided name, shown in the request language; null for a name the user typed.</summary>
    public string? SystemKey { get; set; }

    /// <summary>Lowercase copy of <see cref="Name"/> that backs the unique index.</summary>
    public string NameNormalized { get; private set; } = string.Empty;

    public int Position { get; set; }

    /// <summary>One of <see cref="PipelineStageKinds"/>.</summary>
    public string Kind { get; set; } = PipelineStageKinds.Open;

    /// <summary>Win probability of a deal in this stage, in basis points (10000 = 100 %).</summary>
    public int ProbabilityBps { get; set; }

    /// <summary>Index into the single cool color ramp of the interface.</summary>
    public int ColorIndex { get; set; }

    /// <summary>
    /// Renames the stage. A system name becomes a typed name only when the new text differs from both the stored
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
