using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Exports;
using Parley.Crm.Domain.Notes;
using Parley.Crm.Domain.Orders;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Domain.Tags;
using Parley.Crm.Domain.Tasks;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Outbox.Mapping;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Persistence;

/// <summary>
/// CRM module database context on the <c>crm</c> schema. Every table is a tenant row with
/// the shared tenant conventions; the contact-tag link carries its own tenant filter because
/// it is keyed by the organization and two identifiers. Outbox and inbox rows live here too.
/// </summary>
public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options, ITenantContext tenantContext)
    : DbContext(options), ITenantDbContext
{
    public const string SchemaName = CrmSchemaNames.Schema;

    public ITenantContext TenantContext { get; } = tenantContext;

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<ContactTag> ContactTags => Set<ContactTag>();

    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<ContactExport> ContactExports => Set<ContactExport>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Pipeline> Pipelines => Set<Pipeline>();

    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();

    public DbSet<Deal> Deals => Set<Deal>();

    public DbSet<DealStageHistoryEntry> DealStageHistory => Set<DealStageHistoryEntry>();

    public DbSet<CrmTask> Tasks => Set<CrmTask>();

    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(SchemaName);
        builder.HasPostgresExtension(CrmSchemaNames.TrigramExtension);
        base.OnModelCreating(builder);
        builder.AddTenantConventions(this);
        builder.AddOutboxConventions();
        builder.AddIdempotencyConventions();
        builder.ApplyConfigurationsFromAssembly(typeof(CrmDbContext).Assembly);

        builder.Entity<ContactTag>(entity =>
        {
            entity.ToTable(CrmSchemaNames.ContactTagsTable);
            entity.HasKey(link => new { link.OrganizationId, link.ContactId, link.TagId });
            entity.HasIndex(link => new { link.OrganizationId, link.TagId }).HasDatabaseName(CrmSchemaNames.ContactTagTagIndex);
            entity.HasQueryFilter(link => link.OrganizationId == TenantContext.OrganizationId);
            entity.HasOne<Tag>()
                .WithMany()
                .HasForeignKey(link => link.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
