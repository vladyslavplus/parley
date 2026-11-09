using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Domain.Suppressions;
using Parley.Campaigns.Domain.Templates;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Outbox.Mapping;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Persistence;

/// <summary>
/// Campaigns module database context on the <c>campaigns</c> schema. Every table is a tenant row with the shared
/// tenant conventions. Outbox, inbox and idempotency rows live here too.
/// </summary>
public sealed class CampaignsDbContext(DbContextOptions<CampaignsDbContext> options, ITenantContext tenantContext)
    : DbContext(options), ITenantDbContext
{
    public const string SchemaName = CampaignsSchemaNames.Schema;

    public ITenantContext TenantContext { get; } = tenantContext;

    public DbSet<EmailTemplate> Templates => Set<EmailTemplate>();

    public DbSet<Segment> Segments => Set<Segment>();

    public DbSet<Campaign> Campaigns => Set<Campaign>();

    public DbSet<Recipient> Recipients => Set<Recipient>();

    public DbSet<Suppression> Suppressions => Set<Suppression>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(builder);
        builder.AddTenantConventions(this);
        builder.AddOutboxConventions();
        builder.AddIdempotencyConventions();
        builder.ApplyConfigurationsFromAssembly(typeof(CampaignsDbContext).Assembly);
    }
}
