using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.Notes;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="Note"/>: one subject per note, enforced by a check, and subject-ordered reads.</summary>
public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable(CrmSchemaNames.NotesTable, table =>
            table.HasCheckConstraint(CrmSchemaNames.NoteSubjectCheck, CrmSql.NoteSubjectCheck));
        builder.Property(note => note.Body).HasMaxLength(CrmLimits.NoteBodyMaximumLength).IsRequired();
        builder.Property(note => note.DeletedAt).HasColumnType("timestamptz").HasConversion(UtcValueConverters.Optional);

        builder.HasTenantIndex(CrmSchemaNames.NoteContactIndex, nameof(Note.ContactId), nameof(Note.CreatedAt), nameof(Note.Id));
        builder.HasTenantIndex(CrmSchemaNames.NoteDealIndex, nameof(Note.DealId), nameof(Note.CreatedAt), nameof(Note.Id));
        builder.HasTenantIndex(CrmSchemaNames.NoteCompanyIndex, nameof(Note.CompanyId), nameof(Note.CreatedAt), nameof(Note.Id));
    }
}
