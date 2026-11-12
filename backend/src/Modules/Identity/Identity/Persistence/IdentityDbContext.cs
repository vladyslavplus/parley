using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Domain.Audit;
using Parley.Identity.Domain.DemoSeed;
using Parley.Identity.Domain.Invitations;
using Parley.Identity.Domain.Memberships;
using Parley.Identity.Domain.Organizations;
using Parley.Identity.Domain.Roles;
using Parley.Identity.Domain.Sessions;
using Parley.Identity.Domain.Teams;
using Parley.Identity.Domain.Users;
using Parley.Identity.Shared;
using Parley.SharedKernel.Outbox.Mapping;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Persistence;

/// <summary>
/// Identity module database context on the <c>identity</c> schema. It holds platform
/// rows (users, organizations, refresh tokens, roles and their grants) and the
/// tenant rows of an organization (memberships, teams, invitations, audit events),
/// which get the shared tenant conventions. Outbox rows of the module live here too.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, ITenantContext tenantContext)
    : IdentityUserContext<User, Guid, IdentityUserClaim<Guid>, IdentityUserLogin<Guid>, IdentityUserToken<Guid>>(options), ITenantDbContext
{
    public const string SchemaName = "identity";

    private const string UsersTable = "users";
    private const string UserClaimsTable = "user_claims";
    private const string UserLoginsTable = "user_logins";
    private const string UserTokensTable = "user_tokens";
    private const string OrganizationsTable = "organizations";
    private const string RefreshTokensTable = "refresh_tokens";
    public const string RolesTableName = "roles";
    public const string RolePermissionsTableName = "role_permissions";
    private const string MembershipsTable = "memberships";
    private const string TeamsTable = "teams";
    private const string InvitationsTable = "invitations";
    private const string AuditEventsTable = "audit_events";
    private const string DemoSeedStatesTable = "demo_seed_states";
    private const string TokenHashIndex = "ix_refresh_tokens_token_hash";
    private const string FamilyIndex = "ix_refresh_tokens_family_id";
    private const string UserIndex = "ix_refresh_tokens_user_id";
    private const string DefaultLocaleColumn = "default_locale";
    private const string RoleCodeIndex = "ux_roles_organization_id_code";
    private const string MembershipUserIndex = "ux_memberships_organization_id_user_id";
    private const string MembershipUserLookupIndex = "ix_memberships_user_id";
    private const string MembershipTeamIndex = "ix_memberships_organization_id_team_id";
    private const string MembershipStatusCheck = "ck_memberships_status";
    internal const string TeamNameIndex = "ux_teams_organization_id_name_normalized";

    /// <summary>ASP.NET Core Identity's default unique index on the normalized address.</summary>
    internal const string UserEmailIndex = "EmailIndex";

    /// <summary>ASP.NET Core Identity's default unique index on the normalized user name.</summary>
    internal const string UserNameIndex = "UserNameIndex";
    private const string InvitationTokenIndex = "ux_invitations_token_hash";
    internal const string InvitationPendingIndex = "ux_invitations_organization_id_email_pending";
    private const string InvitationPendingFilter = "\"accepted_at\" IS NULL AND \"revoked_at\" IS NULL";
    private const string RolePermissionScopeCheck = "ck_role_permissions_scope";
    private const string AuditEventIndex = "ux_audit_events_source_event_id";
    private const string NameNormalizedSql = "lower(\"name\")";
    private const string ActiveStatusSqlList = "'active', 'deactivated'";
    private const string ScopeSqlList = "'own', 'team', 'org'";
    private const string OrganizationSlugIndex = "ux_organizations_slug";
    private const string UserThemeCheck = "ck_users_theme";
    private const string ThemeSqlList = "'system', 'light', 'dark'";
    private const string TimeZoneColumn = "time_zone";
    private const string CurrencyColumn = "currency";
    private const string CurrencyColumnType = "char(3)";
    private const string SlugColumn = "slug";

    /// <summary>
    /// Stores timestamps as UTC <c>timestamptz</c>. Npgsql needs <c>Kind=Utc</c>, and
    /// values read back are marked UTC so comparisons stay consistent.
    /// </summary>
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public ITenantContext TenantContext { get; } = tenantContext;

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<DemoSeedState> DemoSeedStates => Set<DemoSeedState>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(builder);
        builder.AddTenantConventions(this);
        builder.AddOutboxConventions();

        builder.Entity<User>(entity =>
        {
            entity.ToTable(UsersTable, table => table.HasCheckConstraint(UserThemeCheck, $"\"theme\" IN ({ThemeSqlList})"));
            entity.Property(user => user.DisplayName).HasMaxLength(IdentityLimits.DisplayNameMaximumLength);
            entity.Property(user => user.Locale).HasMaxLength(IdentityLimits.LocaleMaximumLength).IsRequired();
            entity.Property(user => user.TimeZone).HasColumnName(TimeZoneColumn).HasMaxLength(IdentityLimits.TimeZoneMaximumLength).IsRequired();
            entity.Property(user => user.Theme).HasMaxLength(IdentityLimits.ThemeMaximumLength).IsRequired();
            entity.Property(user => user.IsPlatformOperator).HasDefaultValue(false);
            entity.HasOne<Organization>()
                .WithMany()
                .HasForeignKey(user => user.LastActiveOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<IdentityUserClaim<Guid>>().ToTable(UserClaimsTable);
        builder.Entity<IdentityUserLogin<Guid>>().ToTable(UserLoginsTable);
        builder.Entity<IdentityUserToken<Guid>>().ToTable(UserTokensTable);

        builder.Entity<Organization>(entity =>
        {
            entity.ToTable(OrganizationsTable);
            entity.HasKey(organization => organization.Id);
            entity.Property(organization => organization.Name).HasMaxLength(IdentityLimits.OrganizationNameMaximumLength).IsRequired();
            entity.Property(organization => organization.Slug).HasColumnName(SlugColumn).HasMaxLength(IdentityLimits.SlugMaximumLength).IsRequired();
            entity.HasIndex(organization => organization.Slug).IsUnique().HasDatabaseName(OrganizationSlugIndex);
            entity.Property(organization => organization.TimeZone).HasColumnName(TimeZoneColumn).HasMaxLength(IdentityLimits.TimeZoneMaximumLength).IsRequired();
            entity.Property(organization => organization.DefaultLocale)
                .HasColumnName(DefaultLocaleColumn)
                .HasMaxLength(IdentityLimits.LocaleMaximumLength)
                .IsRequired();
            entity.Property(organization => organization.Currency).HasColumnName(CurrencyColumn).HasColumnType(CurrencyColumnType).IsRequired();
            entity.Property(organization => organization.Status).HasMaxLength(IdentityLimits.StatusMaximumLength).IsRequired();
            entity.Property(organization => organization.CreatedAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.Property(organization => organization.UpdatedAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.Property<uint>(PersistenceConstants.XminPropertyName).IsRowVersion().HasColumnType("xid");
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable(RefreshTokensTable);
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasColumnType("bytea").IsRequired();
            entity.Property(token => token.ExpiresAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.Property(token => token.RevokedAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.Property(token => token.CreatedAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName(TokenHashIndex);
            entity.HasIndex(token => token.FamilyId).HasDatabaseName(FamilyIndex);
            entity.HasIndex(token => token.UserId).HasDatabaseName(UserIndex);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Role>(entity =>
        {
            entity.ToTable(RolesTableName);
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Code).HasMaxLength(IdentityLimits.RoleCodeMaximumLength).IsRequired();
            entity.HasIndex(role => new { role.OrganizationId, role.Code })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasDatabaseName(RoleCodeIndex);
            entity.HasData(SystemRoleSeed.Roles.ToArray());
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable(RolePermissionsTableName, table => table.HasCheckConstraint(RolePermissionScopeCheck, $"\"scope\" IS NULL OR \"scope\" IN ({ScopeSqlList})"));
            entity.HasKey(grant => new { grant.RoleId, grant.Permission });
            entity.Property(grant => grant.Permission).HasMaxLength(IdentityLimits.PermissionMaximumLength).IsRequired();
            entity.Property(grant => grant.Scope).HasMaxLength(IdentityLimits.ScopeMaximumLength);
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(grant => grant.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(SystemRoleSeed.RolePermissions.ToArray());
        });

        builder.Entity<Team>(entity =>
        {
            entity.ToTable(TeamsTable);
            entity.Property(team => team.Name).HasMaxLength(IdentityLimits.TeamNameMaximumLength).IsRequired();
            entity.Property(team => team.NameNormalized)
                .HasMaxLength(IdentityLimits.TeamNameMaximumLength)
                .HasComputedColumnSql(NameNormalizedSql, stored: true);
            entity.HasTenantUniqueIndex(TeamNameIndex, nameof(Team.NameNormalized));
        });

        builder.Entity<Membership>(entity =>
        {
            entity.ToTable(MembershipsTable, table => table.HasCheckConstraint(MembershipStatusCheck, $"\"status\" IN ({ActiveStatusSqlList})"));
            entity.Property(membership => membership.Status).HasMaxLength(IdentityLimits.StatusMaximumLength).IsRequired();
            entity.Property(membership => membership.JoinedAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.HasTenantUniqueIndex(MembershipUserIndex, nameof(Membership.UserId));
            entity.HasIndex(membership => membership.UserId).HasDatabaseName(MembershipUserLookupIndex);
            entity.HasIndex(membership => new { membership.OrganizationId, membership.TeamId }).HasDatabaseName(MembershipTeamIndex);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(membership => membership.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(membership => membership.TeamId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Invitation>(entity =>
        {
            entity.ToTable(InvitationsTable);
            entity.Property(invitation => invitation.EmailNormalized).HasMaxLength(IdentityLimits.EmailMaximumLength).IsRequired();
            entity.Property(invitation => invitation.TokenHash).HasColumnType("bytea").IsRequired();
            entity.Property(invitation => invitation.ExpiresAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.Property(invitation => invitation.AcceptedAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.Property(invitation => invitation.RevokedAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.HasIndex(invitation => invitation.TokenHash).IsUnique().HasDatabaseName(InvitationTokenIndex);
            entity.HasTenantUniqueIndex(InvitationPendingIndex, nameof(Invitation.EmailNormalized))
                .HasFilter(InvitationPendingFilter);
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(invitation => invitation.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(invitation => invitation.TeamId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable(AuditEventsTable);
            entity.Property(auditEvent => auditEvent.Action).HasMaxLength(IdentityLimits.AuditActionMaximumLength).IsRequired();
            entity.Property(auditEvent => auditEvent.TargetType).HasMaxLength(IdentityLimits.TargetTypeMaximumLength);
            entity.Property(auditEvent => auditEvent.Metadata).HasColumnType("jsonb").IsRequired();
            entity.Property(auditEvent => auditEvent.OccurredAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
            entity.HasIndex(auditEvent => auditEvent.SourceEventId).IsUnique().HasDatabaseName(AuditEventIndex);
        });

        builder.Entity<DemoSeedState>(entity =>
        {
            entity.ToTable(DemoSeedStatesTable);
            entity.HasKey(state => state.OrganizationId);
            entity.Property(state => state.OrganizationId).HasColumnName("organization_id").ValueGeneratedNever();
            entity.Property(state => state.SeededAt).HasColumnName("seeded_at").HasColumnType("timestamptz").HasConversion(UtcConverter);
        });
    }
}
