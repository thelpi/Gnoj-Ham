using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel.Services;
using Gnoj_Ham_ViewModel_Tests.Fakes;

namespace Gnoj_Ham_ViewModel_Tests;

public class GameOptionsViewModel_Tests
{
    private readonly FakeUserSettingsStorage _settingsStorage = new();
    private readonly UserSettings _settings = new()
    {
        ChronoSpeed = 1,
        CpuSpeed = 3,
        PlaySounds = true,
        AutoCallMahjong = false
    };

    [Fact]
    public void Constructor_ReadsTheSettingsWithoutSavingThem()
    {
        var options = new GameOptionsViewModel(_settings, _settingsStorage);

        Assert.Equal(1, options.ChronoSpeedIndex);
        Assert.Equal(3, options.CpuSpeedIndex);
        Assert.True(options.PlaySounds);
        Assert.False(options.AutoCallMahjong);
        Assert.Equal(0, _settingsStorage.SaveCount);
    }

    [Fact]
    public void TheChoices_AreTheOnesOfTheIntro()
    {
        var options = new GameOptionsViewModel(_settings, _settingsStorage);

        Assert.Equal(DisplayTexts.GetChronoDisplayValues(), options.ChronoSpeedChoices);
        Assert.Equal(DisplayTexts.GetCpuSpeedDisplayValues(), options.CpuSpeedChoices);
    }

    [Fact]
    public void ChangingTheChrono_SavesItAtOnce()
    {
        var options = new GameOptionsViewModel(_settings, _settingsStorage);

        options.ChronoSpeedIndex = 2;

        Assert.Equal(2, _settings.ChronoSpeed);
        Assert.Equal(1, _settingsStorage.SaveCount);
    }

    [Fact]
    public void ChangingTheCpuSpeed_SavesItAtOnce()
    {
        var options = new GameOptionsViewModel(_settings, _settingsStorage);

        options.CpuSpeedIndex = 0;

        Assert.Equal(0, _settings.CpuSpeed);
        Assert.Equal(1, _settingsStorage.SaveCount);
    }

    [Fact]
    public void ChangingTheSounds_SavesThemAtOnce()
    {
        var options = new GameOptionsViewModel(_settings, _settingsStorage);

        options.PlaySounds = false;

        Assert.False(_settings.PlaySounds);
        Assert.Equal(1, _settingsStorage.SaveCount);
    }

    [Fact]
    public void ChangingTheAutomaticWin_SavesItAtOnce()
    {
        var options = new GameOptionsViewModel(_settings, _settingsStorage);

        options.AutoCallMahjong = true;

        Assert.True(_settings.AutoCallMahjong);
        Assert.Equal(1, _settingsStorage.SaveCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASelectionOnNoChoiceAtAll_IsNotSaved(bool chrono)
    {
        var options = new GameOptionsViewModel(_settings, _settingsStorage);

        if (chrono)
        {
            options.ChronoSpeedIndex = -1;
        }
        else
        {
            options.CpuSpeedIndex = -1;
        }

        Assert.Equal(1, _settings.ChronoSpeed);
        Assert.Equal(3, _settings.CpuSpeed);
        Assert.Equal(0, _settingsStorage.SaveCount);
    }

    [Fact]
    public void Properties_RaisePropertyChangedSoTheViewFollows()
    {
        var options = new GameOptionsViewModel(_settings, _settingsStorage);
        var raised = new List<string?>();
        options.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        options.ChronoSpeedIndex = 2;
        options.CpuSpeedIndex = 0;
        options.PlaySounds = false;
        options.AutoCallMahjong = true;

        Assert.Equal(
            new[]
            {
                nameof(GameOptionsViewModel.ChronoSpeedIndex),
                nameof(GameOptionsViewModel.CpuSpeedIndex),
                nameof(GameOptionsViewModel.PlaySounds),
                nameof(GameOptionsViewModel.AutoCallMahjong)
            },
            raised);
    }
}
