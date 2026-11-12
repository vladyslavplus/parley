using Parley.Campaigns.Domain.DemoSimulation;
using Parley.Crm.Contracts.Metrics;

namespace Parley.Campaigns.Features.Campaigns.DemoSeed;

/// <summary>
/// Maps the CRM customer metrics as of an instant to the eight fields of the demo feature schema (version 1). Recency and
/// tenure are whole days. A field that needs an order is NaN for a contact without orders, so no rule matches it.
/// </summary>
internal static class CampaignDemoFeatures
{
    public static DemoContactFeatures From(CrmCustomerMetricsRow row, DateTimeOffset asOf)
    {
        var recencyDays = row.LastOrderAt is { } last ? Math.Floor((asOf - last).TotalDays) : double.NaN;
        var tenureDays = row.FirstOrderAt is { } first ? Math.Floor((asOf - first).TotalDays) : double.NaN;
        return new DemoContactFeatures(
            recencyDays,
            tenureDays,
            row.Orders90Days,
            row.Orders365Days,
            row.OrderCount,
            (double)row.Value365Days,
            row.OrderCount > 0 ? (double)row.AvgOrderValue : double.NaN,
            row.HasOpenDeal ? 1d : 0d);
    }
}
