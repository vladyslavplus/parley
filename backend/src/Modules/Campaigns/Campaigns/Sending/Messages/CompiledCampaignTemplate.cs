using Fluid;

namespace Parley.Campaigns.Sending.Messages;

/// <summary>A subject and a body parsed by the template sandbox, ready to render for each recipient.</summary>
public sealed record CompiledCampaignTemplate(IFluidTemplate Subject, IFluidTemplate Body);
