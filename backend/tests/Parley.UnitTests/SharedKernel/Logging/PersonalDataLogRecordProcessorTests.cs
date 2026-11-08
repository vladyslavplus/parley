using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Parley.SharedKernel.Logging;
using Xunit;

namespace Parley.UnitTests.SharedKernel.Logging;

/// <summary>
/// Runs records through a real OpenTelemetry logger provider with the
/// processor registered before an in-memory exporter. No pre-fix run: the
/// processor did not exist before this change.
/// </summary>
public sealed class PersonalDataLogRecordProcessorTests
{
    [Fact]
    public void ExceptionWithPersonalData_IsExportedMasked_WithOriginalTypeName()
    {
        var exported = new List<ExportedRecord>();
        using var factory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(options =>
        {
            options.AddProcessor(new PersonalDataLogRecordProcessor());
            options.AddProcessor(new SimpleLogRecordExportProcessor(new SnapshotExporter(exported)));
        }));
        var logger = factory.CreateLogger("Parley.UnitTests");

        logger.LogError(CaptureExceptionWithPersonalData(), "Request failed for alice@example.com");

        var record = exported.Should().ContainSingle().Subject;
        record.Exception.Should().BeNull("the exception object is replaced by masked attributes");
        record.FormattedMessage.Should().NotContain("alice@example.com");

        var attributes = record.Attributes.ToDictionary(attribute => attribute.Key, attribute => attribute.Value);
        attributes[PersonalDataLogRecordProcessor.ExceptionTypeAttributeKey]
            .Should().Be(typeof(InvalidOperationException).FullName, "the original type name is kept");

        var message = (string?)attributes[PersonalDataLogRecordProcessor.ExceptionMessageAttributeKey];
        message.Should().NotContain("alice@example.com", "the outer message is masked");
        message.Should().NotContain("+380675550101", "the inner message is masked");
        message.Should().Contain(PersonalDataMasking.MaskedValue);

        var stackTrace = (string?)attributes[PersonalDataLogRecordProcessor.ExceptionStackTraceAttributeKey];
        stackTrace.Should().NotContain("alice@example.com");
        stackTrace.Should().NotContain("+380675550101");
        stackTrace.Should().Contain(nameof(ThrowWithPersonalData), "stack frames stay readable for diagnosis");

        attributes.Values.OfType<string>().Should().OnlyContain(
            value => !value.Contains("alice@example.com", StringComparison.Ordinal),
            "no string attribute keeps the personal data");
    }

    /// <summary>Copies what the test asserts: exported records are pooled and reused after export.</summary>
    private sealed record ExportedRecord(
        Exception? Exception,
        string? FormattedMessage,
        IReadOnlyList<KeyValuePair<string, object?>> Attributes);

    private sealed class SnapshotExporter(List<ExportedRecord> sink) : BaseExporter<LogRecord>
    {
        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            foreach (var record in batch)
            {
                sink.Add(new ExportedRecord(
                    record.Exception,
                    record.FormattedMessage,
                    (record.Attributes ?? []).ToList()));
            }

            return ExportResult.Success;
        }
    }

    private static Exception CaptureExceptionWithPersonalData()
    {
        try
        {
            ThrowWithPersonalData();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("unreachable");
    }

    private static void ThrowWithPersonalData() =>
        throw new InvalidOperationException(
            "contact alice@example.com",
            new ArgumentException("phone +380675550101"));
}
