using System.Text;

namespace Parley.Campaigns.Templates;

/// <summary>
/// Text writer that stops a render as soon as its output passes the limit, instead of building the whole string first.
/// Every write goes through one length check, so no path can grow the output past the limit.
/// </summary>
public sealed class TemplateOutputWriter(int limit) : TextWriter
{
    private readonly StringBuilder _output = new();

    public override Encoding Encoding => Encoding.UTF8;

    public override string ToString() => _output.ToString();

    public override void Write(char value) => Append(new ReadOnlySpan<char>(in value));

    public override void Write(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Append(value.AsSpan());
        }
    }

    public override void Write(char[] buffer, int index, int count) => Append(buffer.AsSpan(index, count));

    public override void Write(ReadOnlySpan<char> buffer) => Append(buffer);

    public override Task WriteAsync(char value)
    {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(string? value)
    {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        Write(buffer, index, count);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Append(buffer.Span);
        return Task.CompletedTask;
    }

    private void Append(ReadOnlySpan<char> text)
    {
        if (_output.Length + text.Length > limit)
        {
            throw new TemplateOutputLimitException(limit);
        }

        _output.Append(text);
    }
}
