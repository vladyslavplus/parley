using AwesomeAssertions;
using Parley.SharedKernel.Logging;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Parley.UnitTests.SharedKernel.Logging;

public sealed class PersonalDataScrubbingTests
{
    private readonly PersonalDataScrubbingEnricher _enricher = new();

    [Fact]
    public void EmailInsideStructuredValue_IsMasked()
    {
        var logEvent = LogEventWithProperties(
            "{Contact}",
            ("Contact", new StructureValue(
            [
                new LogEventProperty("Name", new ScalarValue("Planned User")),
                new LogEventProperty("Email", new ScalarValue("alice@example.com")),
            ])));

        _enricher.Enrich(logEvent, propertyFactory: null!);

        var rendered = logEvent.RenderMessage();
        rendered.Should().NotContain("alice@example.com", "personal data never reaches logs");
        rendered.Should().Contain(PersonalDataMasking.MaskedValue);
        rendered.Should().Contain("Planned User", "non-personal values stay readable");
    }

    [Fact]
    public void AuthorizationHeaderAndPhoneNumber_AreMasked()
    {
        var logEvent = LogEventWithProperties(
            "{Authorization} {CustomerPhone} {Comment}",
            ("Authorization", new ScalarValue("Bearer abc.def.ghi")),
            ("CustomerPhone", new ScalarValue("+380 67 555 0101")),
            ("Comment", new ScalarValue("Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature")));

        _enricher.Enrich(logEvent, propertyFactory: null!);

        var rendered = logEvent.RenderMessage();
        rendered.Should().NotContain("abc.def.ghi");
        rendered.Should().NotContain("+380 67 555 0101");
        rendered.Should().NotContain("eyJhbGciOiJIUzI1NiJ9");
        rendered.Should().Contain(PersonalDataMasking.MaskedValue);
    }

    [Theory]
    [InlineData("2026-11-08")]
    [InlineData("2026-11-08T12:00:00Z")]
    [InlineData("/api/v1/contacts/018f3c2a-7d1e-7b00-9a4c-2f6e1b9d0a11")]
    [InlineData("order 1234567890")]
    public void DatesAndIdentifiers_AreNotMasked(string value)
    {
        PersonalDataMasking.Mask(value).Should().Be(value,
            "dates and ids are not phone numbers and must stay readable in logs");
    }

    [Fact]
    public void PhoneNumber_WithLeadingPlus_IsMasked()
    {
        PersonalDataMasking.Mask("call +380675550101 now").Should().NotContain("+380675550101");
    }

    [Fact]
    public void RenderedException_MasksOuterAndInnerPersonalData_KeepsStackFrames()
    {
        var exception = CaptureExceptionWithPersonalData();
        var logEvent = new LogEvent(
            new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero),
            LogEventLevel.Error,
            exception,
            new MessageTemplateParser().Parse("Unhandled exception"),
            []);
        var formatter = new PersonalDataScrubbingTextFormatter(
            new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Message:lj}{NewLine}{Exception}", System.Globalization.CultureInfo.InvariantCulture));
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);

        formatter.Format(logEvent, writer);

        var output = writer.ToString();
        output.Should().NotContain("alice@example.com", "the outer exception message is masked");
        output.Should().NotContain("+380675550101", "the inner exception message is masked");
        output.Should().Contain(nameof(ThrowWithPersonalData), "stack frames stay readable for diagnosis");
        output.Should().Contain(PersonalDataMasking.MaskedValue);
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

    private static LogEvent LogEventWithProperties(
        string templateText,
        params (string Name, LogEventPropertyValue Value)[] properties)
    {
        var template = new MessageTemplateParser().Parse(templateText);
        var logProperties = properties.Select(property => new LogEventProperty(property.Name, property.Value));
        return new LogEvent(
            new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero),
            LogEventLevel.Information,
            exception: null,
            template,
            logProperties);
    }
}
