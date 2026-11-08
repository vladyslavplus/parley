using Microsoft.AspNetCore.Identity;
using Parley.Identity.Domain.Users;

namespace Parley.Identity.Authentication.Passwords;

/// <summary>
/// A password hash that matches no account. Sign-in verifies against it when the
/// address is unknown or locked, so those paths cost the same as a wrong password
/// and the response time does not reveal which addresses exist.
/// </summary>
/// <remarks>
/// Owns its own hasher instead of the scoped <see cref="IPasswordHasher{TUser}"/>,
/// so it can live as a singleton. The hash is computed once, on first use.
/// </remarks>
public sealed class DummyPasswordHash
{
    private readonly PasswordHasher<User> _hasher = new();

    private readonly Lazy<string> _hash;

    public DummyPasswordHash()
    {
        _hash = new Lazy<string>(() => _hasher.HashPassword(new User(), Guid.NewGuid().ToString("N")));
    }

    /// <summary>Verifies <paramref name="password"/> against the dummy hash and discards the result.</summary>
    public void Consume(string password)
    {
        _hasher.VerifyHashedPassword(new User(), _hash.Value, password);
    }
}
