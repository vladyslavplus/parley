using AwesomeAssertions;
using Parley.Notifications.Domain.Telegram;
using Xunit;

namespace Parley.UnitTests.Notifications.Telegram;

public sealed class TelegramLinkRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NewCode_HasTheCodeShape_AndIsNotRepeated()
    {
        var first = TelegramLinkRules.NewCode();
        var second = TelegramLinkRules.NewCode();

        first.Should().HaveLength(TelegramLinkRules.CodeLength);
        TelegramLinkRules.HasValidShape(first).Should().BeTrue();
        second.Should().NotBe(first);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData("ABCDEFGHJKMNPQRSTUVWXYZ23456789")]
    [InlineData("0000000000000000000000000000000l")]
    public void HasValidShape_RefusesWrongLengthOrAlphabet_BeforeAnyLookup(string? code) =>
        TelegramLinkRules.HasValidShape(code).Should().BeFalse();

    [Fact]
    public void IsRedeemable_AcceptsAnUnusedCodeBeforeItsExpiry_AndNothingElse()
    {
        var expiresAt = Now.AddMinutes(10);

        TelegramLinkRules.IsRedeemable(expiresAt, usedAt: null, Now).Should().BeTrue();
        TelegramLinkRules.IsRedeemable(expiresAt, usedAt: null, expiresAt).Should().BeFalse("the code expires at its instant");
        TelegramLinkRules.IsRedeemable(expiresAt, usedAt: Now, Now).Should().BeFalse("a code is single use");
    }

    [Fact]
    public void HashCode_IsStableAndNeverTheCodeItself()
    {
        var code = TelegramLinkRules.NewCode();
        var hash = TelegramLinkRules.HashCode(code);

        TelegramLinkRules.HashCode(code).Should().Equal(hash);
        hash.Should().HaveCount(32);
        System.Text.Encoding.UTF8.GetString(hash).Should().NotContain(code);
    }
}
