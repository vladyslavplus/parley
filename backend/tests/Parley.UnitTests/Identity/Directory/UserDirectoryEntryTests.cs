using AwesomeAssertions;
using Parley.Identity.Contracts.Users;
using Xunit;

namespace Parley.UnitTests.Identity.Directory;

public sealed class UserDirectoryEntryTests
{
    [Fact]
    public void Create_TrimsNameAndEmail()
    {
        var userId = Guid.CreateVersion7();

        var entry = UserDirectoryEntry.Create(userId, "  Olena Shevchenko ", " olena@example.test ", isActive: true);

        entry.Should().Be(new UserDirectoryEntry(userId, "Olena Shevchenko", "olena@example.test", IsActive: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReadsBlankValuesAsAbsent(string? blank)
    {
        var userId = Guid.CreateVersion7();

        var entry = UserDirectoryEntry.Create(userId, blank, blank, isActive: false);

        entry.DisplayName.Should().BeNull();
        entry.Email.Should().BeNull();
        entry.UserId.Should().Be(userId);
    }
}
