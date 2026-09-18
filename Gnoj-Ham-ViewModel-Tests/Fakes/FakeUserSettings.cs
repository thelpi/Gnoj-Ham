using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Settings held in memory; counts how many times they were saved.
/// </summary>
internal sealed class FakeUserSettings : IUserSettings
{
    public string DefaultPlayerName { get; set; } = "Player";
    public int ChronoSpeed { get; set; }
    public int CpuSpeed { get; set; }
    public bool PlaySounds { get; set; }
    public bool AutoCallMahjong { get; set; }
    public bool DiscardTip { get; set; }
    public int InitialPointsRule { get; set; }
    public int EndOfGameRule { get; set; }
    public bool UseRedDoras { get; set; }
    public bool UseNagashiMangan { get; set; }
    public bool UseMultipleYakumans { get; set; }
    public bool UseKazoeYakuman { get; set; }
    public bool UseSuufonRenda { get; set; }
    public bool UseDoubleYakuman { get; set; }
    public int UmaRule { get; set; }

    public int SaveCount { get; private set; }

    public void Save() => SaveCount++;
}
