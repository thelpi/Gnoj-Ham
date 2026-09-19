using System.Text.Json;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel.Services;
using Gnoj_Ham_ViewModel_Tests.Fakes;

namespace Gnoj_Ham_ViewModel_Tests;

public class IntroViewModel_Tests
{
    private readonly FakeUserSettingsStorage _settingsStorage = new();
    private readonly UserSettings _settings = new()
    {
        DefaultPlayerName = "Lpi",
        ChronoSpeed = 1,
        CpuSpeed = 2,
        PlaySounds = true,
        AutoCallMahjong = true,
        DiscardTip = true,
        InitialPointsRule = 1,
        EndOfGameRule = 2,
        UseRedDoras = true,
        UseNagashiMangan = true,
        UseMultipleYakumans = false,
        UseKazoeYakuman = false,
        UseSuufonRenda = false,
        UseDoubleYakuman = false,
        UmaRule = 2
    };
    private readonly FakePlayerStatisticsStorage _storage = new();
    private readonly FakeDialogService _dialogs = new();

    private IntroViewModel NewViewModel()
        => new(_settings, _settingsStorage, _storage, _dialogs, new FakeUiDispatcher());

    [Fact]
    public void Creation_LoadsTheSavedConfiguration()
    {
        var viewModel = NewViewModel();

        Assert.Equal("Lpi", viewModel.PlayerName);
        Assert.Equal(1, viewModel.ChronoSpeedIndex);
        Assert.Equal(2, viewModel.CpuSpeedIndex);
        Assert.True(viewModel.PlaySounds);
        Assert.True(viewModel.AutoCallMahjong);
        Assert.True(viewModel.DiscardTip);
        Assert.Equal(1, viewModel.PointsRuleIndex);
        Assert.Equal(2, viewModel.EndOfGameRuleIndex);
        Assert.True(viewModel.UseRedDoras);
        Assert.True(viewModel.UseNagashiMangan);
        Assert.False(viewModel.UseMultipleYakumans);
        Assert.False(viewModel.UseKazoeYakuman);
        Assert.False(viewModel.UseSuufonRenda);
        Assert.False(viewModel.UseDoubleYakuman);
        Assert.Equal(2, viewModel.UmaRuleIndex);
    }

    [Fact]
    public void Creation_LeavesTheDevelopmentToolsOff()
    {
        var viewModel = NewViewModel();

        Assert.False(viewModel.DebugMode);
        Assert.False(viewModel.FourCpus);
        Assert.Equal((int)DrivenDrawScenarios.None, viewModel.DrivenDrawScenarioIndex);
    }

    [Fact]
    public void ChoiceLists_HaveOneEntryPerEnumerationValue()
    {
        var viewModel = NewViewModel();

        Assert.Equal(Enum.GetValues<ChronoPivot>().Length, viewModel.ChronoSpeedChoices.Count);
        Assert.Equal(Enum.GetValues<CpuSpeedPivot>().Length, viewModel.CpuSpeedChoices.Count);
        Assert.Equal(Enum.GetValues<InitialPointsRules>().Length, viewModel.PointsRuleChoices.Count);
        Assert.Equal(Enum.GetValues<EndOfGameRules>().Length, viewModel.EndOfGameRuleChoices.Count);
        Assert.Equal(Enum.GetValues<UmaRules>().Length, viewModel.UmaRuleChoices.Count);
        Assert.Equal(Enum.GetValues<DrivenDrawScenarios>().Length, viewModel.DrivenDrawScenarioChoices.Count);
    }

    [Fact]
    public void Reset_DropsUnsavedChangesAndReloadsTheSavedConfiguration()
    {
        var viewModel = NewViewModel();
        viewModel.PlayerName = "Somebody else";
        viewModel.PlaySounds = false;
        viewModel.UmaRuleIndex = 0;
        viewModel.DebugMode = true;

        viewModel.ResetCommand.Execute(null);

        Assert.Equal("Lpi", viewModel.PlayerName);
        Assert.True(viewModel.PlaySounds);
        Assert.Equal(2, viewModel.UmaRuleIndex);
        Assert.False(viewModel.DebugMode);
        Assert.Equal(0, _settingsStorage.SaveCount);
    }

    [Fact]
    public void ResetRulesToDefault_RestoresTheDefaultRulesAndLeavesTheOptionsAlone()
    {
        var viewModel = NewViewModel();

        viewModel.ResetRulesToDefaultCommand.Execute(null);

        Assert.Equal((int)RulePivot.Default.InitialPointsRule, viewModel.PointsRuleIndex);
        Assert.Equal((int)RulePivot.Default.EndOfGameRule, viewModel.EndOfGameRuleIndex);
        Assert.Equal(RulePivot.Default.UseRedDoras, viewModel.UseRedDoras);
        Assert.Equal(RulePivot.Default.UseNagashiMangan, viewModel.UseNagashiMangan);
        Assert.Equal(RulePivot.Default.UseMultipleYakumans, viewModel.UseMultipleYakumans);
        Assert.Equal(RulePivot.Default.UseKazoeYakuman, viewModel.UseKazoeYakuman);
        Assert.Equal(RulePivot.Default.UseSuufonRenda, viewModel.UseSuufonRenda);
        Assert.Equal(RulePivot.Default.UseDoubleYakuman, viewModel.UseDoubleYakuman);
        Assert.Equal((int)RulePivot.Default.UmaRule, viewModel.UmaRuleIndex);

        Assert.Equal("Lpi", viewModel.PlayerName);
        Assert.Equal(1, viewModel.ChronoSpeedIndex);
    }

    [Fact]
    public void Start_SavesTheConfiguration()
    {
        var viewModel = NewViewModel();
        viewModel.PlayerName = "New name";
        viewModel.CpuSpeedIndex = 4;
        viewModel.UseSuufonRenda = true;
        viewModel.EndOfGameRuleIndex = 3;

        viewModel.StartCommand.Execute(null);

        Assert.Equal("New name", _settings.DefaultPlayerName);
        Assert.Equal(4, _settings.CpuSpeed);
        Assert.True(_settings.UseSuufonRenda);
        Assert.Equal(3, _settings.EndOfGameRule);
        Assert.Equal(1, _settingsStorage.SaveCount);
    }

    [Fact]
    public void Start_OpensAGameWithTheChosenRulesAndOptions()
    {
        _storage.Stats = JsonSerializer.Deserialize<PlayerStatisticsPivot>("{\"GameCount\":3}")!;
        var viewModel = NewViewModel();
        viewModel.DebugMode = true;

        viewModel.StartCommand.Execute(null);

        var setup = Assert.IsType<HumanGameSetup>(Assert.Single(_dialogs.ShownViewModels));
        Assert.Equal("Lpi", setup.PlayerName);
        Assert.Same(_storage.Stats, setup.Stats);
        Assert.True(setup.DebugMode);
        Assert.Null(setup.DrivenDraw);
        Assert.Equal(InitialPointsRules.K30, setup.Ruleset.InitialPointsRule);
        Assert.Equal(EndOfGameRules.Enchousen, setup.Ruleset.EndOfGameRule);
        Assert.True(setup.Ruleset.UseRedDoras);
        Assert.True(setup.Ruleset.UseNagashiMangan);
        Assert.False(setup.Ruleset.UseMultipleYakumans);
        Assert.False(setup.Ruleset.UseKazoeYakuman);
        Assert.False(setup.Ruleset.UseSuufonRenda);
        Assert.False(setup.Ruleset.UseDoubleYakuman);
        Assert.Equal(UmaRules.Ema, setup.Ruleset.UmaRule);
    }

    [Fact]
    public void Start_WithADrivenDrawScenario_RigsTheDraw()
    {
        var viewModel = NewViewModel();
        viewModel.DrivenDrawScenarioIndex = (int)DrivenDrawScenarios.HumanInitialKan;

        viewModel.StartCommand.Execute(null);

        var setup = Assert.IsType<HumanGameSetup>(Assert.Single(_dialogs.ShownViewModels));
        Assert.NotNull(setup.DrivenDraw);
    }

    [Fact]
    public void Start_WithFourCpus_OpensAnAutoPlayBatchInsteadOfAGame()
    {
        var viewModel = NewViewModel();
        viewModel.FourCpus = true;

        viewModel.StartCommand.Execute(null);

        Assert.IsType<AutoPlayViewModel>(Assert.Single(_dialogs.ShownViewModels));
    }

    [Fact]
    public void Start_StepsAsideWhileTheGameIsPlayedThenComesBack()
    {
        var viewModel = NewViewModel();
        var events = new List<string>();
        viewModel.HideRequested += (_, _) => events.Add("hide");
        viewModel.ShowRequested += (_, _) => events.Add("show");
        _dialogs.OnShowDialog = _ => events.Add("game");

        viewModel.StartCommand.Execute(null);

        Assert.Equal(new[] { "hide", "game", "show" }, events);
    }

    [Fact]
    public void Start_ReloadsTheConfigurationOnceTheGameIsOver()
    {
        var viewModel = NewViewModel();
        viewModel.FourCpus = true;
        viewModel.DebugMode = true;
        // The game changes a setting while it is played (e.g. the sound checkbox in the game window).
        _dialogs.OnShowDialog = _ => _settings.PlaySounds = false;

        viewModel.StartCommand.Execute(null);

        Assert.False(viewModel.PlaySounds);
        Assert.False(viewModel.FourCpus);
        Assert.False(viewModel.DebugMode);
    }

    [Fact]
    public void Start_WarnsButStillPlaysWhenTheStatisticsCannotBeLoaded()
    {
        _storage.LoadError = "corrupted file";
        var viewModel = NewViewModel();

        viewModel.StartCommand.Execute(null);

        var (message, _) = Assert.Single(_dialogs.ShownMessages);
        Assert.Contains("corrupted file", message);
        Assert.Contains("ne seront pas sauvegardées", message);
        Assert.IsType<HumanGameSetup>(Assert.Single(_dialogs.ShownViewModels));
    }

    [Fact]
    public void ShowPlayerStats_ShowsTheSavedStatistics()
    {
        _storage.Stats = JsonSerializer.Deserialize<PlayerStatisticsPivot>("{\"GameCount\":9}")!;
        var viewModel = NewViewModel();

        viewModel.ShowPlayerStatsCommand.Execute(null);

        var stats = Assert.IsType<PlayerSaveStatsViewModel>(Assert.Single(_dialogs.ShownViewModels));
        Assert.Equal(9, stats.GamesCount);
        Assert.Empty(_dialogs.ShownMessages);
    }

    [Fact]
    public void ShowPlayerStats_WarnsThatTheStatisticsWillBeEmptyOnALoadError()
    {
        _storage.LoadError = "corrupted file";
        var viewModel = NewViewModel();

        viewModel.ShowPlayerStatsCommand.Execute(null);

        var (message, _) = Assert.Single(_dialogs.ShownMessages);
        Assert.Contains("seront vides", message);
        Assert.IsType<PlayerSaveStatsViewModel>(Assert.Single(_dialogs.ShownViewModels));
    }

    [Fact]
    public void ShowRules_ShowsTheRules()
    {
        var viewModel = NewViewModel();

        viewModel.ShowRulesCommand.Execute(null);

        Assert.IsType<RulesViewModel>(Assert.Single(_dialogs.ShownViewModels));
    }

    [Fact]
    public void ShowAbout_ShowsAMessage()
    {
        var viewModel = NewViewModel();

        viewModel.ShowAboutCommand.Execute(null);

        var (message, title) = Assert.Single(_dialogs.ShownMessages);
        Assert.Equal("Bientôt !", message);
        Assert.Equal("Gnoj-Ham - Information", title);
    }
}
