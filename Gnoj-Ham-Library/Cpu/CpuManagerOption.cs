namespace Gnoj_Ham_Library;

/// <summary>
/// One entry of <see cref="CpuManagerCatalog.Implementations"/>: a concrete
/// <see cref="CpuManagerBasePivot"/> implementation paired with its short display name. A plain class
/// with real properties (rather than a tuple) so it binds cleanly to a WPF <c>DisplayMemberPath</c>.
/// </summary>
/// <param name="Type">The concrete <see cref="CpuManagerBasePivot"/> implementation type.</param>
/// <param name="DisplayName">Short, hardcoded display name (e.g. for a UI picker or a player's name).</param>
public sealed record CpuManagerOption(Type Type, string DisplayName);
