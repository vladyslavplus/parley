using AwesomeAssertions;
using Parley.Crm.Seeding.DemoSeed;
using Xunit;

namespace Parley.UnitTests.Crm.Seeding;

/// <summary>
/// Parity guard for the scoring features: the ML side reproduces these contact identifiers as MD5 of <c>contact:&lt;ref&gt;</c>,
/// so a change to the derivation would silently split the two sides.
/// </summary>
public sealed class DemoIdentifiersTests
{
    [Fact]
    public void ContactIdentifiers_MatchTheValuesTheMlSideReproduces()
    {
        DemoIdentifiers.Derive("contact", "12346").Should().Be(Guid.Parse("d1172cba-2e21-9f00-a831-10772844328e"));
        DemoIdentifiers.Derive("contact", "13085").Should().Be(Guid.Parse("f007c625-6c97-2d3d-542c-0fcbf02142ae"));
    }
}
