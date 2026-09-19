using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests;

public sealed class JsonUserSettingsStorage_Tests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"gnoj-ham-tests-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(_folder, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    [Fact]
    public void Load_WhenNothingIsSaved_GivesTheDefaultSettings()
    {
        var settings = new JsonUserSettingsStorage(FilePath).Load();

        var defaults = new UserSettings();
        Assert.Equal(defaults.DefaultPlayerName, settings.DefaultPlayerName);
        Assert.Equal(defaults.CpuSpeed, settings.CpuSpeed);
        Assert.Equal(defaults.EndOfGameRule, settings.EndOfGameRule);
        Assert.True(settings.AutoCallMahjong);
        Assert.False(settings.DiscardTip);
    }

    [Fact]
    public void Save_ThenLoad_GivesBackEverySetting()
    {
        var storage = new JsonUserSettingsStorage(FilePath);
        var saved = new UserSettings
        {
            DefaultPlayerName = "Someone",
            ChronoSpeed = 1,
            CpuSpeed = 4,
            PlaySounds = false,
            AutoCallMahjong = false,
            DiscardTip = true,
            InitialPointsRule = 1,
            EndOfGameRule = 2,
            UseRedDoras = false,
            UseNagashiMangan = false,
            UseMultipleYakumans = false,
            UseKazoeYakuman = false,
            UseSuufonRenda = false,
            UseDoubleYakuman = false,
            UmaRule = 2
        };

        storage.Save(saved);
        var loaded = new JsonUserSettingsStorage(FilePath).Load();

        Assert.Equal("Someone", loaded.DefaultPlayerName);
        Assert.Equal(1, loaded.ChronoSpeed);
        Assert.Equal(4, loaded.CpuSpeed);
        Assert.False(loaded.PlaySounds);
        Assert.False(loaded.AutoCallMahjong);
        Assert.True(loaded.DiscardTip);
        Assert.Equal(1, loaded.InitialPointsRule);
        Assert.Equal(2, loaded.EndOfGameRule);
        Assert.False(loaded.UseRedDoras);
        Assert.False(loaded.UseNagashiMangan);
        Assert.False(loaded.UseMultipleYakumans);
        Assert.False(loaded.UseKazoeYakuman);
        Assert.False(loaded.UseSuufonRenda);
        Assert.False(loaded.UseDoubleYakuman);
        Assert.Equal(2, loaded.UmaRule);
    }

    [Fact]
    public void Save_CreatesTheFolderOfTheFile()
    {
        new JsonUserSettingsStorage(FilePath).Save(new UserSettings());

        Assert.True(File.Exists(FilePath));
    }

    [Fact]
    public void Load_WhenTheFileLacksSomeSettings_GivesTheDefaultOnesForThem()
    {
        // e.g. a file written by a version before a setting existed.
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, "{\"CpuSpeed\": 4}");

        var settings = new JsonUserSettingsStorage(FilePath).Load();

        Assert.Equal(4, settings.CpuSpeed);
        Assert.Equal(new UserSettings().DefaultPlayerName, settings.DefaultPlayerName);
        Assert.True(settings.UseRedDoras);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("")]
    [InlineData("null")]
    public void Load_WhenTheFileIsUnreadable_GivesTheDefaultSettings(string content)
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, content);

        var settings = new JsonUserSettingsStorage(FilePath).Load();

        Assert.Equal(new UserSettings().CpuSpeed, settings.CpuSpeed);
    }

    [Fact]
    public void Save_WhenTheFileCannotBeWritten_KeepsTheGameGoing()
    {
        // The folder of the file is, in fact, a file.
        Directory.CreateDirectory(_folder);
        var blocker = Path.Combine(_folder, "blocker");
        File.WriteAllText(blocker, string.Empty);

        var exception = Record.Exception(() => new JsonUserSettingsStorage(Path.Combine(blocker, "settings.json")).Save(new UserSettings()));

        Assert.Null(exception);
    }
}
