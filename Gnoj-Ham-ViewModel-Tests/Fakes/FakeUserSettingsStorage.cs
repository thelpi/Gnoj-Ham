using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Keeps nothing but the count of saves.
/// </summary>
internal sealed class FakeUserSettingsStorage : IUserSettingsStorage
{
    public int SaveCount { get; private set; }

    public UserSettings Load() => new();

    public void Save(UserSettings settings) => SaveCount++;
}
