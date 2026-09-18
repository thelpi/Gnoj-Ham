namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// The user's saved settings. Rules and speeds are stored as the index of the chosen value in its
/// enumeration (the same order the choice lists are displayed in).
/// </summary>
public interface IUserSettings
{
    /// <summary>The default human player name.</summary>
    string DefaultPlayerName { get; set; }

    /// <summary>The human decision timer (see <see cref="ChronoPivot"/>).</summary>
    int ChronoSpeed { get; set; }

    /// <summary>The pause between CPU actions (see <see cref="CpuSpeedPivot"/>).</summary>
    int CpuSpeed { get; set; }

    /// <summary>Indicates if sounds are played.</summary>
    bool PlaySounds { get; set; }

    /// <summary>Indicates if ron and tsumo are called automatically as soon as possible.</summary>
    bool AutoCallMahjong { get; set; }

    /// <summary>Indicates if a visual help is given to choose the best decision.</summary>
    bool DiscardTip { get; set; }

    /// <summary>The initial points rule.</summary>
    int InitialPointsRule { get; set; }

    /// <summary>The end of game rule.</summary>
    int EndOfGameRule { get; set; }

    /// <summary>Indicates if red doras are used.</summary>
    bool UseRedDoras { get; set; }

    /// <summary>Indicates if nagashi mangan is used.</summary>
    bool UseNagashiMangan { get; set; }

    /// <summary>Indicates if several distinct yakumans add up in the same hand.</summary>
    bool UseMultipleYakumans { get; set; }

    /// <summary>Indicates if a 13 fans hand without yakuman counts as a yakuman.</summary>
    bool UseKazoeYakuman { get; set; }

    /// <summary>Indicates if the suufon renda abortive draw is used.</summary>
    bool UseSuufonRenda { get; set; }

    /// <summary>Indicates if double yakumans are used.</summary>
    bool UseDoubleYakuman { get; set; }

    /// <summary>The uma rule.</summary>
    int UmaRule { get; set; }

    /// <summary>
    /// Persists the current values.
    /// </summary>
    void Save();
}
