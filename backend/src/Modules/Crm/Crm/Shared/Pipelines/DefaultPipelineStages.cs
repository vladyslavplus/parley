using Parley.Crm.Domain.Pipelines;

namespace Parley.Crm.Shared.Pipelines;

/// <summary>
/// The pipeline every organization starts with: four open stages, then Won and Lost. Names are resource
/// keys, so each organization gets them in its own locale.
/// </summary>
public static class DefaultPipelineStages
{
    public const string PipelineNameKey = "crm.pipeline.default_name";

    private const string NewKey = "crm.pipeline.stage.new";
    private const string QualifiedKey = "crm.pipeline.stage.qualified";
    private const string ProposalKey = "crm.pipeline.stage.proposal";
    private const string NegotiationKey = "crm.pipeline.stage.negotiation";
    private const string WonKey = "crm.pipeline.stage.won";
    private const string LostKey = "crm.pipeline.stage.lost";

    private const int NewProbabilityBps = 1000;
    private const int QualifiedProbabilityBps = 2500;
    private const int ProposalProbabilityBps = 5000;
    private const int NegotiationProbabilityBps = 7500;
    private const int WonProbabilityBps = 10000;
    private const int LostProbabilityBps = 0;

    private const int NewColorIndex = 0;
    private const int QualifiedColorIndex = 1;
    private const int ProposalColorIndex = 2;
    private const int NegotiationColorIndex = 3;
    private const int WonColorIndex = 4;
    private const int LostColorIndex = 0;

    public static IReadOnlyList<DefaultPipelineStage> All { get; } =
    [
        new(NewKey, PipelineStageKinds.Open, NewProbabilityBps, NewColorIndex),
        new(QualifiedKey, PipelineStageKinds.Open, QualifiedProbabilityBps, QualifiedColorIndex),
        new(ProposalKey, PipelineStageKinds.Open, ProposalProbabilityBps, ProposalColorIndex),
        new(NegotiationKey, PipelineStageKinds.Open, NegotiationProbabilityBps, NegotiationColorIndex),
        new(WonKey, PipelineStageKinds.Won, WonProbabilityBps, WonColorIndex),
        new(LostKey, PipelineStageKinds.Lost, LostProbabilityBps, LostColorIndex),
    ];
}
