namespace Parley.Insights.Contracts.MlStatus;

/// <summary>Reads the ML switch of the current organization. An organization without a row has ML off.</summary>
public interface IMlStatusReader
{
    Task<MlStatus> GetAsync(CancellationToken cancellationToken);
}
