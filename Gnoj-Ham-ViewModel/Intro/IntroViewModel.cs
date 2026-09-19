using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// The new game window: options, rules and development tools, saved between sessions, and the entry
/// point to a game.
/// </summary>
public sealed partial class IntroViewModel : ObservableObject
{
    private readonly UserSettings _settings;
    private readonly IUserSettingsStorage _settingsStorage;
    private readonly IPlayerStatisticsStorage _statisticsStorage;
    private readonly IDialogService _dialogs;
    private readonly IUiDispatcher _dispatcher;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="settings">The user's settings.</param>
    /// <param name="settingsStorage">Where the user's settings are kept.</param>
    /// <param name="statisticsStorage">The saved player statistics.</param>
    /// <param name="dialogs">Opens the game and the secondary windows, and shows messages.</param>
    /// <param name="dispatcher">Brings updates from background work back onto the UI thread.</param>
    public IntroViewModel(UserSettings settings, IUserSettingsStorage settingsStorage, IPlayerStatisticsStorage statisticsStorage, IDialogService dialogs, IUiDispatcher dispatcher)
    {
        _settings = settings;
        _settingsStorage = settingsStorage;
        _statisticsStorage = statisticsStorage;
        _dialogs = dialogs;
        _dispatcher = dispatcher;

        LoadConfiguration();
    }

    /// <summary>
    /// Raised when the window should step aside while a game is played.
    /// </summary>
    public event EventHandler? HideRequested;

    /// <summary>
    /// Raised when the window should come back once the game is over.
    /// </summary>
    public event EventHandler? ShowRequested;

    #region Choice lists

    /// <summary>The human decision timer choices.</summary>
    public IReadOnlyList<string> ChronoSpeedChoices { get; } = DisplayTexts.GetChronoDisplayValues();

    /// <summary>The CPU pace choices.</summary>
    public IReadOnlyList<string> CpuSpeedChoices { get; } = DisplayTexts.GetCpuSpeedDisplayValues();

    /// <summary>The initial points rule choices.</summary>
    public IReadOnlyList<string> PointsRuleChoices { get; } = DisplayTexts.GetInitialPointsRuleDisplayValue();

    /// <summary>The end of game rule choices.</summary>
    public IReadOnlyList<string> EndOfGameRuleChoices { get; } = DisplayTexts.GetEndOfGameRuleDisplayValue();

    /// <summary>The uma rule choices.</summary>
    public IReadOnlyList<string> UmaRuleChoices { get; } = DisplayTexts.GetUmaRuleDisplayValue();

    /// <summary>The driven draw scenario choices.</summary>
    public IReadOnlyList<string> DrivenDrawScenarioChoices { get; } = DisplayTexts.GetDrivenDrawScenarioDisplayValue();

    #endregion Choice lists

    #region Options

    /// <summary>The human player name.</summary>
    [ObservableProperty]
    private string _playerName = string.Empty;

    /// <summary>The chosen human decision timer (index in <see cref="ChronoSpeedChoices"/>).</summary>
    [ObservableProperty]
    private int _chronoSpeedIndex;

    /// <summary>The chosen CPU pace (index in <see cref="CpuSpeedChoices"/>).</summary>
    [ObservableProperty]
    private int _cpuSpeedIndex;

    /// <summary>Indicates if sounds are played.</summary>
    [ObservableProperty]
    private bool _playSounds;

    /// <summary>Indicates if ron and tsumo are called automatically as soon as possible.</summary>
    [ObservableProperty]
    private bool _autoCallMahjong;

    /// <summary>Indicates if a visual help is given to choose the best decision.</summary>
    [ObservableProperty]
    private bool _discardTip;

    #endregion Options

    #region Rules

    /// <summary>The chosen initial points rule (index in <see cref="PointsRuleChoices"/>).</summary>
    [ObservableProperty]
    private int _pointsRuleIndex;

    /// <summary>The chosen end of game rule (index in <see cref="EndOfGameRuleChoices"/>).</summary>
    [ObservableProperty]
    private int _endOfGameRuleIndex;

    /// <summary>Indicates if red doras are used.</summary>
    [ObservableProperty]
    private bool _useRedDoras;

    /// <summary>Indicates if nagashi mangan is used.</summary>
    [ObservableProperty]
    private bool _useNagashiMangan;

    /// <summary>Indicates if several distinct yakumans add up in the same hand.</summary>
    [ObservableProperty]
    private bool _useMultipleYakumans;

    /// <summary>Indicates if a 13 fans hand without yakuman counts as a yakuman.</summary>
    [ObservableProperty]
    private bool _useKazoeYakuman;

    /// <summary>Indicates if the suufon renda abortive draw is used.</summary>
    [ObservableProperty]
    private bool _useSuufonRenda;

    /// <summary>Indicates if double yakumans are used.</summary>
    [ObservableProperty]
    private bool _useDoubleYakuman;

    /// <summary>The chosen uma rule (index in <see cref="UmaRuleChoices"/>).</summary>
    [ObservableProperty]
    private int _umaRuleIndex;

    #endregion Rules

    #region Development tools

    /// <summary>Reveals every hand instead of just the human player's.</summary>
    [ObservableProperty]
    private bool _debugMode;

    /// <summary>Plays a batch of games between four CPUs instead of a game with a human player.</summary>
    [ObservableProperty]
    private bool _fourCpus;

    /// <summary>The chosen driven draw scenario (index in <see cref="DrivenDrawScenarioChoices"/>).</summary>
    [ObservableProperty]
    private int _drivenDrawScenarioIndex;

    #endregion Development tools

    // Saves the configuration, steps aside, plays, then comes back: the game might have updated the
    // configuration in the meantime.
    [RelayCommand]
    private void Start()
    {
        SaveConfiguration();

        HideRequested?.Invoke(this, EventArgs.Empty);

        var (stats, error) = _statisticsStorage.Load();

        if (!string.IsNullOrWhiteSpace(error))
        {
            _dialogs.ShowMessage($"Une erreur est survenue pendant le chargement du fichier de statistiques du joueur ; les statistiques ne seront pas sauvegardées.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        var ruleset = new RulePivot
        {
            InitialPointsRule = (InitialPointsRules)PointsRuleIndex,
            EndOfGameRule = (EndOfGameRules)EndOfGameRuleIndex,
            UseRedDoras = UseRedDoras,
            UseNagashiMangan = UseNagashiMangan,
            UseMultipleYakumans = UseMultipleYakumans,
            UseKazoeYakuman = UseKazoeYakuman,
            UseSuufonRenda = UseSuufonRenda,
            UseDoubleYakuman = UseDoubleYakuman,
            UmaRule = (UmaRules)UmaRuleIndex
        };

        if (FourCpus)
        {
            _dialogs.ShowDialog(new AutoPlayViewModel(ruleset, _dialogs, _dispatcher));
        }
        else
        {
            var drivenDraw = DrivenDrawPivot.Resolve((DrivenDrawScenarios)DrivenDrawScenarioIndex, PlayerIndices.Zero);
            _dialogs.ShowDialog(new HumanGameSetup(PlayerName, ruleset, stats, drivenDraw, DebugMode));
        }

        LoadConfiguration();

        ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    // Reloads the last saved configuration, dropping this session's unsaved changes.
    [RelayCommand]
    private void Reset()
    {
        LoadConfiguration();
    }

    // Puts the rules back to their true defaults, whatever was last saved.
    [RelayCommand]
    private void ResetRulesToDefault()
    {
        PointsRuleIndex = (int)RulePivot.Default.InitialPointsRule;
        EndOfGameRuleIndex = (int)RulePivot.Default.EndOfGameRule;
        UseRedDoras = RulePivot.Default.UseRedDoras;
        UseNagashiMangan = RulePivot.Default.UseNagashiMangan;
        UseMultipleYakumans = RulePivot.Default.UseMultipleYakumans;
        UseKazoeYakuman = RulePivot.Default.UseKazoeYakuman;
        UseSuufonRenda = RulePivot.Default.UseSuufonRenda;
        UseDoubleYakuman = RulePivot.Default.UseDoubleYakuman;
        UmaRuleIndex = (int)RulePivot.Default.UmaRule;
    }

    [RelayCommand]
    private void ShowPlayerStats()
    {
        var (stats, error) = _statisticsStorage.Load();

        if (!string.IsNullOrWhiteSpace(error))
        {
            _dialogs.ShowMessage($"Une erreur est survenue pendant le chargement du fichier de statistiques du joueur ; les statistiques seront vides.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        _dialogs.ShowDialog(new PlayerSaveStatsViewModel(stats));
    }

    [RelayCommand]
    private void ShowRules()
    {
        _dialogs.ShowDialog(new RulesViewModel());
    }

    [RelayCommand]
    private void ShowAbout()
    {
        _dialogs.ShowMessage("Bientôt !", "Gnoj-Ham - Information");
    }

    private void LoadConfiguration()
    {
        // Rules
        PointsRuleIndex = _settings.InitialPointsRule;
        EndOfGameRuleIndex = _settings.EndOfGameRule;
        UseRedDoras = _settings.UseRedDoras;
        UseNagashiMangan = _settings.UseNagashiMangan;
        UseMultipleYakumans = _settings.UseMultipleYakumans;
        UseKazoeYakuman = _settings.UseKazoeYakuman;
        UseSuufonRenda = _settings.UseSuufonRenda;
        UseDoubleYakuman = _settings.UseDoubleYakuman;
        UmaRuleIndex = _settings.UmaRule;

        // Options
        PlayerName = _settings.DefaultPlayerName;
        ChronoSpeedIndex = _settings.ChronoSpeed;
        CpuSpeedIndex = _settings.CpuSpeed;
        PlaySounds = _settings.PlaySounds;
        AutoCallMahjong = _settings.AutoCallMahjong;
        DiscardTip = _settings.DiscardTip;

        // Development tools
        DebugMode = false;
        FourCpus = false;
        DrivenDrawScenarioIndex = (int)DrivenDrawScenarios.None;
    }

    private void SaveConfiguration()
    {
        _settings.DefaultPlayerName = PlayerName;
        _settings.ChronoSpeed = ChronoSpeedIndex;
        _settings.CpuSpeed = CpuSpeedIndex;
        _settings.PlaySounds = PlaySounds;
        _settings.AutoCallMahjong = AutoCallMahjong;
        _settings.DiscardTip = DiscardTip;

        _settings.InitialPointsRule = PointsRuleIndex;
        _settings.EndOfGameRule = EndOfGameRuleIndex;
        _settings.UseRedDoras = UseRedDoras;
        _settings.UseNagashiMangan = UseNagashiMangan;
        _settings.UseMultipleYakumans = UseMultipleYakumans;
        _settings.UseKazoeYakuman = UseKazoeYakuman;
        _settings.UseSuufonRenda = UseSuufonRenda;
        _settings.UseDoubleYakuman = UseDoubleYakuman;
        _settings.UmaRule = UmaRuleIndex;

        _settingsStorage.Save(_settings);
    }
}
