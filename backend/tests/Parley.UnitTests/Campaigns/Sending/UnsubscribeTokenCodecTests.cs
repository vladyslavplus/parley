using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Options;
using Parley.Campaigns.Sending.Unsubscribe;
using Xunit;

namespace Parley.UnitTests.Campaigns.Sending;

public sealed class UnsubscribeTokenCodecTests
{
    private const string Key = "unit-test-unsubscribe-key-0123456789";

    private const string OtherKey = "another-unit-test-key-0123456789ab";

    private static readonly Guid Organization = Guid.Parse("0192f5a0-7b1c-7000-8000-0000000000a1");

    private static readonly Guid Campaign = Guid.Parse("0192f5a0-7b1c-7000-8000-0000000000a2");

    private static readonly Guid Contact = Guid.Parse("0192f5a0-7b1c-7000-8000-0000000000a3");

    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IssuedToken_VerifiesToItsOrganizationCampaignAndContact()
    {
        var clock = new SettableTimeProvider(Now);
        var token = Codec(Key, clock).Issue(Organization, Campaign, Contact);

        var verification = Codec(Key, clock).Verify(token);

        verification.Check.Should().Be(UnsubscribeTokenCheck.Valid);
        verification.Claims.Should().NotBeNull();
        verification.Claims!.OrganizationId.Should().Be(Organization);
        verification.Claims.CampaignId.Should().Be(Campaign);
        verification.Claims.ContactId.Should().Be(Contact);
    }

    [Fact]
    public void ForgedOrChangedToken_IsInvalid()
    {
        var clock = new SettableTimeProvider(Now);
        var token = Codec(Key, clock).Issue(Organization, Campaign, Contact);
        var parts = token.Split('.');

        // Signed with another key: a forger without the secret cannot produce a valid signature.
        var forged = Codec(OtherKey, clock).Issue(Organization, Campaign, Contact);
        Codec(Key, clock).Verify(forged).Check.Should().Be(UnsubscribeTokenCheck.Invalid);

        // The payload of a valid token is changed (another contact): the signature no longer matches.
        var otherContact = Codec(Key, clock).Issue(Organization, Campaign, Guid.CreateVersion7());
        var swapped = otherContact.Split('.')[0] + "." + parts[1];
        Codec(Key, clock).Verify(swapped).Check.Should().Be(UnsubscribeTokenCheck.Invalid);

        // The signature is changed by one character.
        var lastCharacter = parts[1][^1];
        var altered = parts[0] + "." + parts[1][..^1] + (lastCharacter == 'A' ? 'B' : 'A');
        Codec(Key, clock).Verify(altered).Check.Should().Be(UnsubscribeTokenCheck.Invalid);
    }

    [Fact]
    public void TokenPastItsLifetime_IsExpired()
    {
        var clock = new SettableTimeProvider(Now);
        var token = Codec(Key, clock).Issue(Organization, Campaign, Contact);

        clock.Advance(TimeSpan.FromDays(2));

        Codec(Key, clock).Verify(token).Check.Should().Be(UnsubscribeTokenCheck.Expired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-separator")]
    [InlineData("too.many.parts")]
    [InlineData("!!!.@@@")]
    public void MalformedInput_IsInvalid_AndNeverThrows(string? token)
    {
        var clock = new SettableTimeProvider(Now);

        Codec(Key, clock).Verify(token).Check.Should().Be(UnsubscribeTokenCheck.Invalid);
    }

    private static UnsubscribeTokenCodec Codec(string key, TimeProvider clock) =>
        new(Options.Create(new CampaignsSendOptions
        {
            UnsubscribeTokenKey = key,
            UnsubscribeTokenLifetime = TimeSpan.FromDays(1),
        }), clock);

    private sealed class SettableTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now = _now.Add(by);
    }
}
