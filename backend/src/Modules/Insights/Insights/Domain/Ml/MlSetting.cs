using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Insights.Domain.Ml;

/// <summary>The ML switch of an organization. One row per organization; the version is the row's xmin.</summary>
public sealed class MlSetting : BusinessEntity
{
    public bool MlEnabled { get; set; }

    public string? ActiveUpliftVersion { get; set; }

    public string? ActiveClvVersion { get; set; }

    public Guid? UpdatedBy { get; set; }
}
