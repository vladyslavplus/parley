using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Parley.Notifications.Persistence;

/// <summary>
/// Maps the parameters of a notification to its <c>jsonb</c> column. The dictionary is written as a <see cref="JsonDocument"/>,
/// which Npgsql writes as jsonb natively; a plain dictionary needs dynamic JSON, which the module does not enable.
/// </summary>
public sealed class NotificationParametersConverter() : ValueConverter<Dictionary<string, string>, JsonDocument>(
    parameters => JsonDocument.Parse(JsonSerializer.Serialize(parameters, JsonSerializerOptions.Default)),
    document => document.Deserialize<Dictionary<string, string>>(JsonSerializerOptions.Default) ?? new Dictionary<string, string>());
