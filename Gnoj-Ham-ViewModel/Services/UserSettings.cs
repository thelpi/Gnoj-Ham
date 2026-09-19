namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// The user's settings, with the values used until they are changed. One instance is shared by every
/// view-model, so a change made by one is seen by the others; <see cref="IUserSettingsStorage"/> keeps
/// it between sessions. Rules and speeds are stored as the index of the chosen value in its
/// enumeration (the same order the choice lists are displayed in).
/// </summary>
public sealed class UserSettings
{
    /// <summary>The default human player name.</summary>
    public string DefaultPlayerName { get; set; } = "Lpi";

    /// <summary>The human decision timer (see <see cref="ChronoPivot"/>).</summary>
    public int ChronoSpeed { get; set; }

    /// <summary>The pause between CPU actions (see <see cref="CpuSpeedPivot"/>).</summary>
    public int CpuSpeed { get; set; } = 2;

    /// <summary>Indicates if sounds are played.</summary>
    public bool PlaySounds { get; set; } = true;

    /// <summary>Indicates if ron and tsumo are called automatically as soon as possible.</summary>
    public bool AutoCallMahjong { get; set; } = true;

    /// <summary>Indicates if a visual help is given to choose the best decision.</summary>
    public bool DiscardTip { get; set; }

    /// <summary>The initial points rule.</summary>
    public int InitialPointsRule { get; set; }

    /// <summary>The end of game rule.</summary>
    public int EndOfGameRule { get; set; } = 3;

    /// <summary>Indicates if red doras are used.</summary>
    public bool UseRedDoras { get; set; } = true;

    /// <summary>Indicates if nagashi mangan is used.</summary>
    public bool UseNagashiMangan { get; set; } = true;

    /// <summary>Indicates if several distinct yakumans add up in the same hand.</summary>
    public bool UseMultipleYakumans { get; set; } = true;

    /// <summary>Indicates if a 13 fans hand without yakuman counts as a yakuman.</summary>
    public bool UseKazoeYakuman { get; set; } = true;

    /// <summary>Indicates if the suufon renda abortive draw is used.</summary>
    public bool UseSuufonRenda { get; set; } = true;

    /// <summary>Indicates if double yakumans are used.</summary>
    public bool UseDoubleYakuman { get; set; } = true;

    /// <summary>The uma rule.</summary>
    public int UmaRule { get; set; }
}
