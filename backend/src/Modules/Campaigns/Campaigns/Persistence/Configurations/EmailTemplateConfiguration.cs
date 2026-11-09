using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Campaigns.Domain.Templates;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Campaigns.Persistence.Configurations;

/// <summary>Mapping of <see cref="EmailTemplate"/>.</summary>
public sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable(CampaignsSchemaNames.TemplatesTable);
        builder.Property(template => template.Name).HasMaxLength(CampaignsLimits.NameMaximumLength).IsRequired();
        builder.Property(template => template.SubjectLiquid).HasMaxLength(CampaignsLimits.SubjectMaximumLength).IsRequired();
        builder.Property(template => template.BodyLiquid).HasColumnType("text").IsRequired();
        builder.Property(template => template.Locale).HasMaxLength(CampaignsLimits.LocaleMaximumLength).IsRequired();
        builder.Property(template => template.ArchivedAt)
            .HasColumnType("timestamptz")
            .HasConversion(CampaignsUtcValueConverters.Optional);
        builder.HasTenantIndex(CampaignsSchemaNames.TemplateListIndex, nameof(EmailTemplate.Name), nameof(EmailTemplate.Id));
    }
}
