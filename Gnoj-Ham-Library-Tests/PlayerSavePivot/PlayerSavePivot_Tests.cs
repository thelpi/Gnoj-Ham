using System.Reflection;
using System.Text.Json;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class PlayerSavePivot_Tests
{
    // Stats only have a private setter (by design); reflection mirrors what UpdateAndSave does internally.
    private static void SetStat(PlayerSavePivot save, string propertyName, object? value)
        => typeof(PlayerSavePivot).GetProperty(propertyName)!.SetValue(save, value);

    [Fact]
    public void JsonRoundTrip_SurvivesSerializationAndDeserialization()
    {
        // Regression test: PlayerSavePivot's stats are "{ get; private set; }", and System.Text.Json
        // silently ignores non-public setters on deserialization unless told otherwise ([JsonInclude]).
        // Without it, every stat silently resets to its default on reload, and the very next save then
        // permanently overwrites the file with that reset state.
        var save = new PlayerSavePivot();
        SetStat(save, nameof(PlayerSavePivot.FirstGame), new DateTime(2020, 1, 1));
        SetStat(save, nameof(PlayerSavePivot.LastGame), new DateTime(2020, 6, 15));
        SetStat(save, nameof(PlayerSavePivot.GameCount), 7);
        SetStat(save, nameof(PlayerSavePivot.RoundCount), 42);
        SetStat(save, nameof(PlayerSavePivot.ByPositionCount), new[] { 1, 2, 3, 4 });
        SetStat(save, nameof(PlayerSavePivot.RiichiCount), 5);
        SetStat(save, nameof(PlayerSavePivot.BankruptCount), 1);
        SetStat(save, nameof(PlayerSavePivot.TsumoCount), 8);
        SetStat(save, nameof(PlayerSavePivot.RonCount), 6);
        SetStat(save, nameof(PlayerSavePivot.YakumanCount), 2);
        SetStat(save, nameof(PlayerSavePivot.OpenedHandCount), 3);

        var json = JsonSerializer.Serialize(save);
        var reloaded = JsonSerializer.Deserialize<PlayerSavePivot>(json);

        Assert.NotNull(reloaded);
        Assert.Equal(save.FirstGame, reloaded.FirstGame);
        Assert.Equal(save.LastGame, reloaded.LastGame);
        Assert.Equal(save.GameCount, reloaded.GameCount);
        Assert.Equal(save.RoundCount, reloaded.RoundCount);
        Assert.Equal(save.ByPositionCount, reloaded.ByPositionCount);
        Assert.Equal(save.RiichiCount, reloaded.RiichiCount);
        Assert.Equal(save.BankruptCount, reloaded.BankruptCount);
        Assert.Equal(save.TsumoCount, reloaded.TsumoCount);
        Assert.Equal(save.RonCount, reloaded.RonCount);
        Assert.Equal(save.YakumanCount, reloaded.YakumanCount);
        Assert.Equal(save.OpenedHandCount, reloaded.OpenedHandCount);
    }
}
