using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Parley.Campaigns.Domain.Segments;

namespace Parley.Campaigns.Persistence;

/// <summary>
/// Maps a segment definition to a <c>jsonb</c> column. The definition is serialized to a <see cref="JsonDocument"/>, which
/// Npgsql writes as jsonb natively. A stored value that reads back as null is an error, not an open definition: an open
/// definition would match every contact, so reading it fails closed.
/// </summary>
public static class SegmentDefinitionJsonConverter
{
    public static ValueConverter<SegmentDefinition, JsonDocument> Instance { get; } = new(
        definition => JsonSerializer.SerializeToDocument(definition),
        document => ReadDefinition(document));

    private static SegmentDefinition ReadDefinition(JsonDocument document) =>
        document.RootElement.Deserialize<SegmentDefinition>()
            ?? throw new InvalidDataException("A stored segment definition is null.");
}
