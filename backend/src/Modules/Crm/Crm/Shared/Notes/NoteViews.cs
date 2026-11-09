using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Notes;
using Parley.Crm.Features.Notes.Shared;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Users;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Shared.Notes;

/// <summary>Projection and mapping of notes. The author name comes from the user directory, one call per page.</summary>
public static class NoteViews
{
    public static Expression<Func<Note, NoteRow>> RowProjection() => note => new NoteRow(
        note.Id,
        note.ContactId,
        note.DealId,
        note.CompanyId,
        note.Body,
        note.AuthorUserId,
        note.CreatedAt,
        note.UpdatedAt,
        EF.Property<uint>(note, PersistenceConstants.XminPropertyName));

    /// <summary>Maps one row, resolving its author with a single directory call.</summary>
    public static async Task<NoteView> ToViewAsync(NoteRow row, OwnerDirectory owners, CancellationToken cancellationToken) =>
        ToView(row, await owners.ReadAsync([(Guid?)row.AuthorUserId], cancellationToken));

    public static NoteView ToView(NoteRow row, IReadOnlyDictionary<Guid, UserDirectoryEntry> authors)
    {
        var author = OwnerDirectory.Find(authors, row.AuthorUserId);
        return new NoteView(
            row.Id,
            row.ContactId,
            row.DealId,
            row.CompanyId,
            row.Body,
            row.AuthorUserId,
            author?.DisplayName,
            author?.Email,
            row.CreatedAt,
            row.UpdatedAt,
            row.Version);
    }
}
