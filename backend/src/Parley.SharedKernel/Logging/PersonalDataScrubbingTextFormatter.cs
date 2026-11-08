using Serilog.Events;
using Serilog.Formatting;

namespace Parley.SharedKernel.Logging;

/// <summary>
/// Text formatter wrapper that masks personal data in the rendered log event:
/// the message, every property and the whole exception chain (outer and inner
/// messages and stack frames). Stack frames stay readable for diagnosis; only
/// the personal data inside them is replaced. Wrapping the sink formatter
/// covers every event that reaches the Serilog pipeline, including events from
/// libraries that log exception objects (Hangfire, EF Core, Npgsql).
/// </summary>
public sealed class PersonalDataScrubbingTextFormatter(ITextFormatter inner) : ITextFormatter
{
    private readonly ITextFormatter _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        using var rendered = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        _inner.Format(logEvent, rendered);
        output.Write(PersonalDataMasking.Mask(rendered.ToString()));
    }
}
