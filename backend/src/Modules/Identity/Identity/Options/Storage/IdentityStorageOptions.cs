namespace Parley.Identity.Options.Storage;

/// <summary>
/// Storage switches for the identity schema. Bind to the <c>IdentityStorage</c>
/// section.
/// </summary>
public sealed class IdentityStorageOptions
{
    public const string SectionName = "IdentityStorage";

    /// <summary>
    /// Applies pending identity migrations when the host starts. Honored only in
    /// the Development environment; other environments migrate through a
    /// deployment step.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
