using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Parley.SharedKernel.Logging;

/// <summary>
/// OpenTelemetry log processor that masks personal data before any exporter
/// sees a record. It must be registered before the exporters. It masks the
/// body, the formatted message, string attribute values and the exception.
///
/// The exception is not replaced by a substitute object: OTLP derives
/// <c>exception.type</c> from the runtime type, so a substitute would report
/// its own type. Instead the exception is removed from the record and its
/// semantic-convention fields are written as masked attributes:
/// <c>exception.type</c> keeps the original type name, <c>exception.message</c>
/// holds the masked message chain and <c>exception.stacktrace</c> the masked
/// <c>ToString()</c> text (inner exceptions and stack frames included).
/// </summary>
public sealed class PersonalDataLogRecordProcessor : BaseProcessor<LogRecord>
{
    public const string ExceptionTypeAttributeKey = "exception.type";

    public const string ExceptionMessageAttributeKey = "exception.message";

    public const string ExceptionStackTraceAttributeKey = "exception.stacktrace";

    private const int MaximumExceptionChainDepth = 16;

    public override void OnEnd(LogRecord data)
    {
        ArgumentNullException.ThrowIfNull(data);

        data.Body = MaskText(data.Body);
        data.FormattedMessage = MaskText(data.FormattedMessage);
        data.Attributes = MaskAttributes(data.Attributes, data.Exception);
        data.Exception = null;
    }

    private static string? MaskText(string? value) =>
        string.IsNullOrEmpty(value) ? value : PersonalDataMasking.Mask(value);

    private static IReadOnlyList<KeyValuePair<string, object?>>? MaskAttributes(
        IReadOnlyList<KeyValuePair<string, object?>>? attributes,
        Exception? exception)
    {
        if (attributes is null && exception is null)
        {
            return attributes;
        }

        var masked = new List<KeyValuePair<string, object?>>((attributes?.Count ?? 0) + 3);
        if (attributes is not null)
        {
            foreach (var attribute in attributes)
            {
                masked.Add(new KeyValuePair<string, object?>(
                    attribute.Key,
                    attribute.Value is string text ? PersonalDataMasking.Mask(text) : attribute.Value));
            }
        }

        if (exception is not null)
        {
            masked.Add(new KeyValuePair<string, object?>(ExceptionTypeAttributeKey, exception.GetType().FullName));
            masked.Add(new KeyValuePair<string, object?>(ExceptionMessageAttributeKey, MaskedMessageChain(exception)));
            masked.Add(new KeyValuePair<string, object?>(
                ExceptionStackTraceAttributeKey,
                PersonalDataMasking.Mask(exception.ToString())));
        }

        return masked;
    }

    private static string MaskedMessageChain(Exception exception)
    {
        var messages = new List<string>();
        var current = exception;
        while (current is not null && messages.Count < MaximumExceptionChainDepth)
        {
            messages.Add(PersonalDataMasking.Mask(current.Message));
            current = current.InnerException;
        }

        return string.Join(" ---> ", messages);
    }
}
