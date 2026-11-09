namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>Counts of one demo import. Contains no personal data.</summary>
/// <param name="ContactsRead">Contacts in the input file.</param>
/// <param name="ContactsInserted">Contacts created by this run.</param>
/// <param name="ContactsSkipped">Contacts that already existed (same source and external reference).</param>
/// <param name="OrdersRead">Orders in the input file.</param>
/// <param name="OrdersInserted">Orders created by this run.</param>
/// <param name="OrdersSkipped">Orders that already existed.</param>
/// <param name="OrdersWithoutContact">Orders whose customer is not in the contacts file; excluded.</param>
/// <param name="ContactsLastOrderUpdated">Contacts whose last order instant changed in this run.</param>
/// <param name="LatestOrderAt">Latest order instant after the date shift, UTC; null without orders.</param>
/// <param name="PipelineCreated">True when this run created the default pipeline of the organization.</param>
/// <param name="CompaniesInserted">Companies created by this run.</param>
/// <param name="DealsInserted">Deals created by this run.</param>
/// <param name="DealStageHistoryInserted">Stage history rows created by this run.</param>
/// <param name="TasksInserted">Tasks created by this run.</param>
/// <param name="NotesInserted">Notes created by this run.</param>
/// <param name="ActivitiesInserted">Timeline entries created by this run.</param>
public sealed record CrmDemoImportReport(
    int ContactsRead,
    int ContactsInserted,
    int ContactsSkipped,
    int OrdersRead,
    int OrdersInserted,
    int OrdersSkipped,
    int OrdersWithoutContact,
    int ContactsLastOrderUpdated,
    DateTimeOffset? LatestOrderAt,
    bool PipelineCreated,
    int CompaniesInserted,
    int DealsInserted,
    int DealStageHistoryInserted,
    int TasksInserted,
    int NotesInserted,
    int ActivitiesInserted);
