namespace Parley.Api.Options;

/// <summary>
/// PostgreSQL connection for the modular monolith. Overridable by an environment variable
/// such as <c>Database__ConnectionString</c>.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;
}
