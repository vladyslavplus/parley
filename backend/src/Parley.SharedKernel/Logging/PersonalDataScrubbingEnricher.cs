using Serilog.Core;
using Serilog.Events;

namespace Parley.SharedKernel.Logging;

/// <summary>
/// Enricher that keeps personal data out of logs (emails, phone numbers,
/// tokens, authorization headers). It walks every log property, masks string
/// values that look like personal data, and masks whole values whose property
/// name is sensitive, recursing into destructured structures, sequences and
/// dictionaries. Scalar values are immutable, so scrubbed values replace the
/// originals on the log event.
///
/// Register with Serilog as <c>.Enrich.With&lt;PersonalDataScrubbingEnricher&gt;()</c>.
/// </summary>
public sealed class PersonalDataScrubbingEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        foreach (var (name, value) in logEvent.Properties)
        {
            var scrubbed = ScrubValue(name, value);
            if (!ReferenceEquals(scrubbed, value))
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(name, scrubbed));
            }
        }
    }

    private static LogEventPropertyValue ScrubValue(string propertyName, LogEventPropertyValue value)
    {
        if (PersonalDataMasking.IsSensitivePropertyName(propertyName))
        {
            return new ScalarValue(PersonalDataMasking.MaskedValue);
        }

        switch (value)
        {
            case ScalarValue { Value: string text }:
            {
                var masked = PersonalDataMasking.Mask(text);
                return ReferenceEquals(masked, text) ? value : new ScalarValue(masked);
            }

            case StructureValue structure:
            {
                var properties = new LogEventProperty[structure.Properties.Count];
                var changed = false;
                for (var index = 0; index < structure.Properties.Count; index++)
                {
                    var property = structure.Properties[index];
                    var scrubbed = ScrubValue(property.Name, property.Value);
                    changed |= !ReferenceEquals(scrubbed, property.Value);
                    properties[index] = ReferenceEquals(scrubbed, property.Value)
                        ? property
                        : new LogEventProperty(property.Name, scrubbed);
                }

                return changed ? new StructureValue(properties, structure.TypeTag) : value;
            }

            case SequenceValue sequence:
            {
                var values = new LogEventPropertyValue[sequence.Elements.Count];
                var changed = false;
                for (var index = 0; index < sequence.Elements.Count; index++)
                {
                    var scrubbed = ScrubValue(propertyName, sequence.Elements[index]);
                    changed |= !ReferenceEquals(scrubbed, sequence.Elements[index]);
                    values[index] = scrubbed;
                }

                return changed ? new SequenceValue(values) : value;
            }

            case DictionaryValue dictionary:
            {
                var entries = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>(dictionary.Elements.Count);
                var changed = false;
                foreach (var (key, entryValue) in dictionary.Elements)
                {
                    var scrubbed = ScrubValue(propertyName, entryValue);
                    changed |= !ReferenceEquals(scrubbed, entryValue);
                    entries.Add(new KeyValuePair<ScalarValue, LogEventPropertyValue>(key, scrubbed));
                }

                return changed ? new DictionaryValue(entries) : value;
            }

            default:
                return value;
        }
    }
}
