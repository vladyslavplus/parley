using System.Diagnostics;
using AwesomeAssertions;
using Parley.Notifications.Telegram.Bot;
using Xunit;

namespace Parley.UnitTests.Observability;

public sealed class TelegramTraceRedactionTests
{
    private const string Token = "123:ABC";

    [Fact]
    public void TelegramCall_EndsWithoutTheTokenInAnyTag()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref _) => ActivitySamplingResult.AllData;
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("Parley.UnitTests.Telegram");
        using var activity = source.StartActivity("POST");
        activity.Should().NotBeNull();

        var requestUri = new Uri($"https://api.telegram.org/bot{Token}/sendMessage");
        activity!.SetTag("url.full", requestUri.ToString());
        activity.SetTag("url.path", requestUri.AbsolutePath);
        activity.SetTag("http.url", requestUri.ToString());
        TelegramTraceRedaction.Redact(activity, requestUri);

        activity.Tags.Select(tag => tag.Value).Should().NotContain(value => value != null && value.Contains(Token));
        activity.GetTagItem("url.path").Should().Be("/bot***/sendMessage");
        activity.GetTagItem("url.full").Should().Be("https://api.telegram.org/bot***/sendMessage");
    }

    [Fact]
    public void OtherHosts_KeepTheirUrl()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref _) => ActivitySamplingResult.AllData;
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("Parley.UnitTests.Telegram");
        using var activity = source.StartActivity("GET");
        activity.Should().NotBeNull();

        var requestUri = new Uri("https://mail.example.test/api/send");
        activity!.SetTag("url.path", requestUri.AbsolutePath);
        TelegramTraceRedaction.Redact(activity, requestUri);

        activity.GetTagItem("url.path").Should().Be("/api/send");
    }
}
