using AwesomeAssertions;
using Parley.Notifications.Domain.Telegram.Commands;
using Xunit;

namespace Parley.UnitTests.Notifications.Telegram;

public sealed class TelegramCommandParserTests
{
    [Theory]
    [InlineData("/start abc123", TelegramCommandKind.Start, "abc123")]
    [InlineData("/start", TelegramCommandKind.Start, null)]
    [InlineData("/today", TelegramCommandKind.Today, null)]
    [InlineData("/today@ParleyBot 2", TelegramCommandKind.Today, "2")]
    [InlineData("/TODAY", TelegramCommandKind.Today, null)]
    [InlineData("/unlink", TelegramCommandKind.Unlink, null)]
    [InlineData("/help", TelegramCommandKind.Help, null)]
    [InlineData("  /help  ", TelegramCommandKind.Help, null)]
    [InlineData("hello there", TelegramCommandKind.Unknown, null)]
    [InlineData("/unknown thing", TelegramCommandKind.Unknown, null)]
    [InlineData("", TelegramCommandKind.Unknown, null)]
    [InlineData(null, TelegramCommandKind.Unknown, null)]
    public void Parse_ReadsTheCommandAndItsOneArgument(string? text, TelegramCommandKind kind, string? argument)
    {
        var command = TelegramCommandParser.Parse(text);

        command.Kind.Should().Be(kind);
        command.Argument.Should().Be(argument);
    }
}
