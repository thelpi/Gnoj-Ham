using CommunityToolkit.Mvvm.ComponentModel;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// The options the player can change while the game is on. Unlike the ones of the intro, which wait for
/// the game to start, each change takes effect - and is saved - at once.
/// </summary>
public sealed partial class GameOptionsViewModel : ObservableObject
{
    private readonly UserSettings _settings;
    private readonly IUserSettingsStorage _storage;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="settings">The user's settings.</param>
    /// <param name="storage">Where the user's settings are kept.</param>
    public GameOptionsViewModel(UserSettings settings, IUserSettingsStorage storage)
    {
        _settings = settings;
        _storage = storage;

        // Read straight into the fields: what is read is not a change to save again.
        _chronoSpeedIndex = settings.ChronoSpeed;
        _cpuSpeedIndex = settings.CpuSpeed;
        _playSounds = settings.PlaySounds;
        _autoCallMahjong = settings.AutoCallMahjong;
    }

    /// <summary>
    /// The choices of human decision timer.
    /// </summary>
    public IReadOnlyList<string> ChronoSpeedChoices { get; } = DisplayTexts.GetChronoDisplayValues();

    /// <summary>
    /// The choices of pause between CPU actions.
    /// </summary>
    public IReadOnlyList<string> CpuSpeedChoices { get; } = DisplayTexts.GetCpuSpeedDisplayValues();

    /// <summary>
    /// The chosen human decision timer (index in <see cref="ChronoSpeedChoices"/>).
    /// </summary>
    [ObservableProperty]
    private int _chronoSpeedIndex;

    /// <summary>
    /// The chosen pause between CPU actions (index in <see cref="CpuSpeedChoices"/>).
    /// </summary>
    [ObservableProperty]
    private int _cpuSpeedIndex;

    /// <summary>
    /// Indicates if sounds are played.
    /// </summary>
    [ObservableProperty]
    private bool _playSounds;

    /// <summary>
    /// Indicates if ron and tsumo are called automatically as soon as possible.
    /// </summary>
    [ObservableProperty]
    private bool _autoCallMahjong;

    // A list being filled or emptied can leave its selection on no choice at all, which is not a choice.
    partial void OnChronoSpeedIndexChanged(int value)
    {
        if (value >= 0)
        {
            _settings.ChronoSpeed = value;
            _storage.Save(_settings);
        }
    }

    partial void OnCpuSpeedIndexChanged(int value)
    {
        if (value >= 0)
        {
            _settings.CpuSpeed = value;
            _storage.Save(_settings);
        }
    }

    partial void OnPlaySoundsChanged(bool value)
    {
        _settings.PlaySounds = value;
        _storage.Save(_settings);
    }

    partial void OnAutoCallMahjongChanged(bool value)
    {
        _settings.AutoCallMahjong = value;
        _storage.Save(_settings);
    }
}
