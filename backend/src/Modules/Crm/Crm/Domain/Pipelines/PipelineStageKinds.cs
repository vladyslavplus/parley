namespace Parley.Crm.Domain.Pipelines;

/// <summary>Kinds of pipeline stage. A new value is a code change.</summary>
public static class PipelineStageKinds
{
    public const string Open = "open";

    public const string Won = "won";

    public const string Lost = "lost";

    /// <summary>True for the stages that close a deal.</summary>
    public static bool IsTerminal(string kind) => kind is Won or Lost;
}
